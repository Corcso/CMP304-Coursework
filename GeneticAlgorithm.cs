using Godot;
using System;
using System.Collections.Generic;

public partial class GeneticAlgorithm : Node
{
	[Export] PackedScene FishTemplate;

	Fish[] thisGeneration;
	Fish[] toCreateNextGeneration;
	int generation;

	RandomNumberGenerator rng;

	[Export] float generationLifetime;

	Func<Fish, float> FitnessFunction;

	double timeElapsed = 0;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		thisGeneration = new Fish[500];
		toCreateNextGeneration = new Fish[20];
		rng = new RandomNumberGenerator();

		// Summon initial generation
		for (int i = 0; i < 500; i++)
		{
			thisGeneration[i] = FishTemplate.Instantiate<Fish>();
			thisGeneration[i].LoadRandom();
			thisGeneration[i].Position = new Vector2(-500, rng.RandiRange(-300, 300));
			GetNode<Node>("../Fish Tank").AddChild(thisGeneration[i]);
		}


		// Define fitness function
		FitnessFunction = (Fish toEvaluate) =>
		{
			return (toEvaluate.Position.X + 1000.0f) / 2000.0f;
		};
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		timeElapsed += delta;
		if (timeElapsed > generationLifetime) { 
			PickEvolvedFish();
			QueueFree();
		}
	}

	public void PickEvolvedFish() {
		Array.Sort(thisGeneration, Comparer<Fish>.Create((Fish f1, Fish f2) => { return (FitnessFunction(f1) > FitnessFunction(f2)) ? 1 : -1; }));

        for (int i = 0; i < 20; i++)
        {
			toCreateNextGeneration[i] = thisGeneration[i + 480];
			GD.Print(toCreateNextGeneration[i].Position.X);
        }

        for (int i = 0; i < 500; i++)
        {
			thisGeneration[i].alive = false;
        }
    }

	public void NewGeneration() { 
	
	}


}
