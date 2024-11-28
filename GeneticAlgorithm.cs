using Godot;
using System;
using System.Collections.Generic;

public partial class GeneticAlgorithm : Node
{
	[Export] PackedScene FishTemplate;

	public Fish[] thisGeneration;
    ulong[] toCreateNextGeneration;
	public int generation = 1;

	RandomNumberGenerator rng;

	[Export] public int squareGenerationSize;
	[Export] public float mutationPercentage;

	// Define fitness function in use and all the available ones
	Func<Fish, float> FitnessFunction;

    // Define recombination function in use and all the available ones
	public Func<ulong, ulong, ulong> RecombinationFunction;
	public Func<ulong, ulong, ulong> SinglePointCrossover;
	public Func<ulong, ulong, ulong> DoublePointCrossover;
	public Func<ulong, ulong, ulong> ChromosomeMidpoint;
	public Func<ulong, ulong, ulong> GeneMidpoints;

    // Define mutation function in use and all the available ones
    Func<ulong, ulong> MutationFunction;


    [Export] int renderEveryXTicks = 60;
    [Export] public int maxTicks = 5000;
	public int ticksThisGeneration = 0;


	public enum State { PRE_SIM, SIMULATING, PAUSED};
	State currentState;

	public float bestFitness;
	public float avgFitness;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		//DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
		Engine.PhysicsTicksPerSecond = 60;

		
		rng = new RandomNumberGenerator();

		// Set current state
		currentState = State.PRE_SIM;

		// Define fitness function
		FitnessFunction = (Fish toEvaluate) =>
		{
			if (!toEvaluate.alive) return 0;
			return (toEvaluate.Position.X + 1000.0f) / 2000.0f;
		};

		// Setup recombination functions
		SinglePointCrossover = (ulong parent1, ulong parent2) => {
            // Get the swap after point (the left boundary)
            int swapAfter = 64 - rng.RandiRange(0, 64);
            // Turn it into a bit mask, if we had 8 bits, a swap after of 3 would be 0000 0111
            ulong mask = 0xFFFFFFFFFFFFFFFF >> swapAfter;

            // Take parent1's DNA from inside the mask, and parent2's DNA from outside.
            ulong childGene = (parent1 & mask) | (parent2 & ~mask);
			return childGene;
		};

        DoublePointCrossover = (ulong parent1, ulong parent2) => {
			// Get the swap after point (the left boundary)
            int swapAfter = 64 - rng.RandiRange(0, 64);
			// Turn it into a bit mask, if we had 8 bits, a swap after of 3 would be 0000 0111
            ulong afterMask = 0xFFFFFFFFFFFFFFFF >> swapAfter;

			// Get the swap before point (the right boundary)
			// This is in distance from the right instead of the left, 
			// Cant be more than swap after
            int swapBefore = rng.RandiRange(0, swapAfter - 1);
			// Turn it into a bit mask, if we had 8 bits, a swap before of 1 would be 1111 1110
            ulong beforeMask = 0xFFFFFFFFFFFFFFFF << swapBefore;

			// And the two masks together to get our recombination/crossover/swapping mask
			// 0000 0111 and 1111 1110 is 0000 0110 which is what we would expect. 
			ulong swapMask = afterMask & beforeMask;

			// Take parent1's DNA from inside the mask, and parent2's DNA from outside.
            ulong childGene = (parent1 & swapMask) | (parent2 & ~swapMask);
            return childGene;
        };

		ChromosomeMidpoint = (ulong parent1, ulong parent2) => {
			// Get the difference between the two chromosomes numerically. 
			// Make sure no data is lost as we cant get any more resolution (already using 64 bit numbers)
			bool p1High = (parent1 > parent2);
			// Get the difference making sure its positive
			ulong difference = p1High ? parent1 - parent2 : parent2 - parent1;
			// Half the difference
			difference /= 2;

			// Add the half difference back onto the lowest of the 2 chromosomes. Return it. 
            return p1High ? parent2 + difference : parent1 + difference;

			// We cant do (parent1 + parent2) / 2 as adding them could cause a overflow. 
        };

		RecombinationFunction = SinglePointCrossover; // Set the default

		GeneMidpoints = (ulong parent1, ulong parent2) => {
            // Get the midpoint of each gene, we can use (gene1 + gene2) / 2 as each gene is only 1 byte
             byte leftMovement =      (byte)((((parent1 & 0x0000FF0000000000) >> 40) + ((parent2 & 0x0000FF0000000000) >> 40)) / 2);
             byte rightMovement =     (byte)((((parent1 & 0x000000FF00000000) >> 32) + ((parent2 & 0x000000FF00000000) >> 32)) / 2);
             byte length =            (byte)((((parent1 & 0x00000000FF000000) >> 24) + ((parent2 & 0x00000000FF000000) >> 24)) / 2);
             byte upperMuscle =       (byte)((((parent1 & 0x0000000000FF0000) >> 16) + ((parent2 & 0x0000000000FF0000) >> 16)) / 2);
             byte lowerMuscle =       (byte)((((parent1 & 0x000000000000FF00) >> 8) +  ((parent2 & 0x000000000000FF00) >> 8)) / 2);
             byte tailHeight =        (byte)((((parent1 & 0x00000000000000FF) >> 0) +  ((parent2 & 0x00000000000000FF) >> 0)) / 2);

			// Combine these genes back into a chromosome and return
			ulong child = 0;
			child |= (ulong)leftMovement << 40;
			child |= (ulong)rightMovement << 32;
			child |= (ulong)length << 24;
			child |= (ulong)upperMuscle << 16;
			child |= (ulong)lowerMuscle << 8;
			child |= (ulong)tailHeight << 0;

			return child;
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
			generation++;
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

		// Get best and average fitness
		bestFitness = FitnessFunction(thisGeneration[(squareGenerationSize * squareGenerationSize) - 1]);
		float totalFitness = 0;
        for (int i = 0; i < squareGenerationSize * squareGenerationSize; i++)
        {
			totalFitness += FitnessFunction(thisGeneration[i]);
        }
		avgFitness = totalFitness / (squareGenerationSize * squareGenerationSize);

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

        thisGeneration = new Fish[squareGenerationSize * squareGenerationSize];
        toCreateNextGeneration = new ulong[squareGenerationSize];

        // Summon initial generation
        for (int i = 0; i < squareGenerationSize * squareGenerationSize; i++)
        {
            thisGeneration[i] = FishTemplate.Instantiate<Fish>();
            thisGeneration[i].LoadRandom();
            thisGeneration[i].Position = new Vector2(-500, rng.RandiRange(-300, 300));
            thisGeneration[i].GA = this;
            GetNode<Node>("../Fish Tank").AddChild(thisGeneration[i]);
        }

        generation = 0;
        ticksThisGeneration = 0;

       
    }

	public void Pause() 
	{
		currentState = State.PAUSED;

        RenderingServer.RenderLoopEnabled = true;
    }

    public void Unpause()
    {
        currentState = State.SIMULATING;

        RenderingServer.RenderLoopEnabled = false;
    }

    public void End()
    {
        currentState = State.PRE_SIM;

        // Delete current generation
        for (int i = 0; i < squareGenerationSize * squareGenerationSize; i++)
        {
			thisGeneration[i].QueueFree();
			thisGeneration[i] = null;
        }

        RenderingServer.RenderLoopEnabled = true;

        
    }

	public State GetCurrentState() {
		return currentState;
	}

}
