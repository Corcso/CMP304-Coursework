using Godot;
using System;
using System.Collections.Generic;

public partial class GeneticAlgorithm : Node
{
	[Export] PackedScene FishTemplate;

	Fish[] thisGeneration;
	int generation;

	RandomNumberGenerator rng;

	[Export] float generationLifetime; 

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
		thisGeneration = new Fish[500];
		rng = new RandomNumberGenerator();

		// Summon initial generation
		for (int i = 0; i < 500; i++)
		{
			thisGeneration[i] = FishTemplate.Instantiate<Fish>();
			thisGeneration[i].LoadRandom();
			thisGeneration[i].Position = new Vector2(-500, rng.RandiRange(-300, 300));
			GetNode<Node>("../Fish Tank").AddChild(thisGeneration[i]);
		}
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

	public void NewGeneration() { 
	
	}


}
