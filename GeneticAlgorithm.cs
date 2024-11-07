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

	[Export] float generationLifetime;

	Func<Fish, float> FitnessFunction;
    Func<ulong, ulong, ulong> RecombinationFunction;
	Func<Fish, Fish> MutationFunction;

	double timeElapsed = 0;

	[Export] Label FPS;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);
		Engine.PhysicsTicksPerSecond = 300;
		Engine.TimeScale = 3;

		thisGeneration = new Fish[400];
		toCreateNextGeneration = new ulong[20];
		rng = new RandomNumberGenerator();

		// Summon initial generation
		for (int i = 0; i < 400; i++)
		{
			thisGeneration[i] = FishTemplate.Instantiate<Fish>();
			thisGeneration[i].LoadRandom();
			thisGeneration[i].Position = new Vector2(-500, rng.RandiRange(-300, 300));
			GetNode<Node>("../Fish Tank").AddChild(thisGeneration[i]);
		}


		// Define fitness function
		FitnessFunction = (Fish toEvaluate) =>
		{
			if (!toEvaluate.alive) return 0;
			return (toEvaluate.Position.X + 1000.0f) / 2000.0f;
		};

		RecombinationFunction = (ulong parent1, ulong parent2) => { 
            //ulong parentGene1 = parent1.GetGene();
			//ulong parentGene2 = parent2.GetGene();

			int swapAfter = 48 - rng.RandiRange(0, 48);
			ulong mask = 0xFFFFFFFFFFFFFFFF >> swapAfter;

			ulong childGene = (parent1 & mask) | (parent2 & ~mask);
			return childGene;
			//child.LoadGene(childGene);
            //GetNode<Node>("../Fish Tank").AddChild(child);
		};
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		FPS.Text = "FPS: " + Engine.GetFramesPerSecond();

		timeElapsed += delta;
		if (timeElapsed > generationLifetime) {
            NewGeneration();
			timeElapsed = 0;
		}
	}

	

	public void PickEvolvedFish() {
		//GD.Print(thisGeneration[0].GetGene());
		Array.Sort(thisGeneration, Comparer<Fish>.Create((Fish f1, Fish f2) => { 
			return (FitnessFunction(f1) > FitnessFunction(f2)) ? 1 : ((FitnessFunction(f1) == FitnessFunction(f2)) ? 0 : -1); 
		}));

        for (int i = 0; i < 20; i++)
        {
			toCreateNextGeneration[i] = thisGeneration[i + 380].GetGene();
			//GD.Print(toCreateNextGeneration[i].Position.X);
        }

        for (int i = 0; i < 400; i++)
        {
			thisGeneration[i].alive = false;
			thisGeneration[i].Reset();
        }
    }

	public void CreateNewPopulation()
	{
		for (int i = 0; i < 20; i++)
		{
            for (int j = 0; j < 20; j++)
            {
				if (i == j)
				{
					thisGeneration[i * 20 + j].LoadGene(toCreateNextGeneration[i]);
				}
				else {
					thisGeneration[i * 20 + j].LoadGene(RecombinationFunction(toCreateNextGeneration[i], toCreateNextGeneration[j]));
                    //RecombinationFunction(toCreateNextGeneration[i], toCreateNextGeneration[j], thisGeneration[i * 20 + j]);
                }
			}
        }
    }

	public void SpawnNewPopulation() {
        for (int i = 0; i < 400; i++)
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


}
