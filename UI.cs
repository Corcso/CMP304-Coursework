using Godot;
using System;
using Range = Godot.Range;

public partial class UI : Control
{
    // GUI ELEMENTS
    Label currentTick_Label;
    Label currentGeneration_Label;
    Label lastBestFitness_Label;
    Label lastAverageFitness_Label;

    Label squareGenerationToActualSize_Label;

    // Need a reference to mutation rate slider and box as they are conencted
    Range mutationRateSlider;
    Range mutationRateBox;

    // Reference to the GeneticAlgorithm so paramaters can be set. 
    GeneticAlgorithm GA;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        // Get GA
        GA = GetNode<GeneticAlgorithm>("../Genetic Algorithm");

        // Setup output
        currentTick_Label = GetNode<Label>("./Currently Playing UI/Panel/Ticks");
        currentGeneration_Label = GetNode<Label>("./Currently Playing UI/Panel/Generation");
        lastBestFitness_Label = GetNode<Label>("./Currently Playing UI/Panel/Last Best Fitness");
        lastAverageFitness_Label = GetNode<Label>("./Currently Playing UI/Panel/Last Mean Fitness");
        squareGenerationToActualSize_Label = GetNode<Label>("./Settings Menu/Panel/Actual Generation Size");

        // Setup signals (for input)
        GetNode<Button>("./Currently Playing UI/Pause Button").Pressed += () => { GA.Pause(); };
        GetNode<Button>("./Currently Playing UI/Stop Button").Pressed += () => { GA.End(); };

        GetNode<Button>("./Settings Menu/Panel/Start Button").Pressed += () => { GA.Start(); };

        GetNode<Range>("./Settings Menu/Panel/Square Generation Size").ValueChanged += (double value) => {
            GA.squareGenerationSize = (int)value;
            squareGenerationToActualSize_Label.Text = ((int)(value * value)).ToString() + " Fish";
        };

        GetNode<Range>("./Settings Menu/Panel/Lifetime").ValueChanged += (double value) => {
            GA.maxTicks = (int)value;
        };

        mutationRateSlider = GetNode<Range>("./Settings Menu/Panel/Mutation Rate Slider");
        mutationRateBox = GetNode<Range>("./Settings Menu/Panel/Mutation Rate Box");
        mutationRateSlider.ValueChanged += (double value) => {
            GA.mutationPercentage = (float)value / 100.0f;
            if(mutationRateBox.Value != value) mutationRateBox.Value  = value;
        };
        mutationRateBox.ValueChanged += (double value) => {
            GA.mutationPercentage = (float)value / 100.0f;
            if (mutationRateSlider.Value != value) mutationRateSlider.Value = value;
        };


    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
