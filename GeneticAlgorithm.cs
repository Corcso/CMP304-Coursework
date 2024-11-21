using Godot;
using System;
using System.Collections.Generic;

public partial class GeneticAlgorithm : Node
{
	[Export] PackedScene FishTemplate;

	Fish[] thisGeneration;
    ulong[] toCreateNextGeneration;
	int generation;

	RandomNumberGenerator rng;

	[Export] public int squareGenerationSize;
	[Export] public float mutationPercentage;

	Func<Fish, float> FitnessFunction;
    Func<ulong, ulong, ulong> RecombinationFunction;
	Func<ulong, ulong> MutationFunction;


    [Export] int renderEveryXTicks = 60;
    [Export] public int maxTicks = 5000;
	int ticksThisGeneration = 0;


	enum State { PRE_SIM, SIMULATING, PAUSED};
	State currentState;


    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		//DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
		Engine.PhysicsTicksPerSecond = 60;

		thisGeneration = new Fish[squareGenerationSize * squareGenerationSize];
		toCreateNextGeneration = new ulong[squareGenerationSize];
		rng = new RandomNumberGenerator();

		// Set current state
		currentState = State.PRE_SIM;

		// Define fitness function
		FitnessFunction = (Fish toEvaluate) =>
		{
			if (!toEvaluate.alive) return 0;
			return (toEvaluate.Position.X + 1000.0f) / 2000.0f;
		};

		RecombinationFunction = (ulong parent1, ulong parent2) => { 
            //ulong parentGene1 = parent1.GetGene();
			//ulong parentGene2 = parent2.GetGene();

			int swapAfter = 64 - rng.RandiRange(0, 64);
			ulong mask = 0xFFFFFFFFFFFFFFFF >> swapAfter;

			ulong childGene = (parent1 & mask) | (parent2 & ~mask);
			return childGene;
			//child.LoadGene(childGene);
            //GetNode<Node>("../Fish Tank").AddChild(child);
		};

		MutationFunction = (ulong unMutatedGene) =>
		{
			int swapBit = rng.RandiRange(0, 63);
			ulong swapMask = (ulong)0x0000000000000001 << swapBit;

			return unMutatedGene ^ swapMask;
		};
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		if(currentState != State.SIMULATING) return;
		if (ticksThisGeneration > maxTicks) {
            NewGeneration();
            ticksThisGeneration = 0;
		}

		if(ticksThisGeneration % renderEveryXTicks == 0) { RenderingServer.RenderLoopEnabled = true; }
		if(ticksThisGeneration % renderEveryXTicks == 1) { RenderingServer.RenderLoopEnabled = false; }

        ticksThisGeneration++;
	}

	

	public void PickEvolvedFish() {
		//GD.Print(thisGeneration[0].GetGene());
		Array.Sort(thisGeneration, Comparer<Fish>.Create((Fish f1, Fish f2) => { 
			return (FitnessFunction(f1) > FitnessFunction(f2)) ? 1 : ((FitnessFunction(f1) == FitnessFunction(f2)) ? 0 : -1); 
		}));

        for (int i = 0; i < squareGenerationSize; i++)
        {
			toCreateNextGeneration[i] = thisGeneration[i + (squareGenerationSize * squareGenerationSize) - squareGenerationSize].GetGene();
			//GD.Print(toCreateNextGeneration[i].Position.X);
        }

        for (int i = 0; i < squareGenerationSize * squareGenerationSize; i++)
        {
			thisGeneration[i].alive = false;
			thisGeneration[i].Reset();
        }
    }

	public void CreateNewPopulation()
	{
		for (int i = 0; i < squareGenerationSize; i++)
		{
            for (int j = 0; j < squareGenerationSize; j++)
            {
				if (i == j)
				{
					thisGeneration[i * squareGenerationSize + j].LoadGene(toCreateNextGeneration[i]);
				}
				else {
					ulong childGene = RecombinationFunction(toCreateNextGeneration[i], toCreateNextGeneration[j]);
					float mutationRoll = rng.Randf();
                    if(mutationRoll < mutationPercentage) thisGeneration[i * squareGenerationSize + j].LoadGene(MutationFunction(childGene));
                    else thisGeneration[i * squareGenerationSize + j].LoadGene(childGene);
                    //RecombinationFunction(toCreateNextGeneration[i], toCreateNextGeneration[j], thisGeneration[i * 20 + j]);
                }
			}
        }
    }

	public void SpawnNewPopulation() {
        for (int i = 0; i < squareGenerationSize * squareGenerationSize; i++)
        {
            thisGeneration[i].Position = new Vector2(-500, rng.RandiRange(-300, 300));
			thisGeneration[i].alive = true;
			thisGeneration[i].Show();
			if (thisGeneration[i].GetParentOrNull<Node>() == null)
			{
				GetNode<Node>("../Fish Tank").AddChild(thisGeneration[i]);
			}
        }
    }

	public void NewGeneration() {
		PickEvolvedFish();
        //thisGeneration = new Fish[400];
        CreateNewPopulation();
		SpawnNewPopulation();

    }

	public void Start()
	{
        currentState = State.SIMULATING;

        // Summon initial generation
        for (int i = 0; i < squareGenerationSize * squareGenerationSize; i++)
        {
            thisGeneration[i] = FishTemplate.Instantiate<Fish>();
            thisGeneration[i].LoadRandom();
            thisGeneration[i].Position = new Vector2(-500, rng.RandiRange(-300, 300));
            GetNode<Node>("../Fish Tank").AddChild(thisGeneration[i]);
        }
    }

	public void Pause() 
	{
		currentState = State.PAUSED;
	}

    public void End()
    {
        currentState = State.PRE_SIM;

        // Delete current generation
        for (int i = 0; i < squareGenerationSize * squareGenerationSize; i++)
        {
			thisGeneration[i].QueueFree();
        }
    }

}
