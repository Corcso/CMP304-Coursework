using Godot;
using System;
using Range = Godot.Range;

public partial class UI : Control
{
    // Pages
    Control settingsPage;
    Control simulationPage;
    Control fishViewPage;

    // GUI ELEMENTS
    Label currentTick_Label;
    Label currentGeneration_Label;
    Label lastBestFitness_Label;
    Label lastAverageFitness_Label;

    Label squareGenerationToActualSize_Label;

    Label fishNumber_Label;
    RichTextLabel fishBarcode_Label;
    RichTextLabel fishGenes_Label;

    // Need a reference to mutation rate slider and box as they are conencted
    Range mutationRateSlider;
    Range mutationRateBox;

    // Current fish that the fish overview box is on
    Fish fishOnOverview;

    // Reference to the GeneticAlgorithm so paramaters can be set. 
    GeneticAlgorithm GA;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        // Get GA
        GA = GetNode<GeneticAlgorithm>("../Genetic Algorithm");

        // Set pages
        settingsPage = GetNode<Control>("./Settings Menu");
        simulationPage = GetNode<Control>("./Currently Playing UI");
        fishViewPage = GetNode<Control>("./Single Fish View");

        // Setup output
        currentTick_Label = GetNode<Label>("./Currently Playing UI/Panel/Ticks");
        currentGeneration_Label = GetNode<Label>("./Currently Playing UI/Panel/Generation");
        lastBestFitness_Label = GetNode<Label>("./Currently Playing UI/Panel/Last Best Fitness");
        lastAverageFitness_Label = GetNode<Label>("./Currently Playing UI/Panel/Last Mean Fitness");
        squareGenerationToActualSize_Label = GetNode<Label>("./Settings Menu/Panel/Actual Generation Size");
        fishNumber_Label = GetNode<Label>("./Single Fish View/Panel/Fish Name");
        fishBarcode_Label = GetNode<RichTextLabel>("./Single Fish View/Panel/Fish Chromosome");
        fishGenes_Label = GetNode<RichTextLabel>("./Single Fish View/Panel/Fish Genes");

        // Setup signals (for input)
        GetNode<Button>("./Currently Playing UI/Pause Button").Pressed += () => {
            if (GA.GetCurrentState() != GeneticAlgorithm.State.PAUSED)
            {
                GA.Pause();
                GetNode<Button>("./Currently Playing UI/Pause Button").Text = "Resume Simulation";
            }
            else {
                GA.Unpause();
                GetNode<Button>("./Currently Playing UI/Pause Button").Text = "Pause Simulation";
            }
            
        
        };
        GetNode<Button>("./Currently Playing UI/Stop Button").Pressed += () => { 
            GA.End();
            settingsPage.Show();
            simulationPage.Hide();
        };

        GetNode<Button>("./Settings Menu/Panel/Start Button").Pressed += () => { 
            GA.Start();
            settingsPage.Hide();
            simulationPage.Show();
        };

        GetNode<Button>("./Single Fish View/Panel/Close Button").Pressed += () => {
            fishOnOverview = null;
            fishViewPage.Hide();
        };

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
        currentTick_Label.Text = "Tick: " + GA.ticksThisGeneration.ToString();
        currentGeneration_Label.Text = "Gen: " + GA.generation.ToString();
        lastBestFitness_Label.Text = "Best Fitness: " + MathF.Round(GA.bestFitness, 3).ToString();
        lastAverageFitness_Label.Text = "Avg. Fitness: " + MathF.Round(GA.avgFitness, 3).ToString();
	}

    public void ShowFishOverview(Fish fishToShow)
    {
        fishOnOverview = fishToShow;
        fishViewPage.Show();
        fishNumber_Label.Text = "Fish: " + Array.IndexOf(GA.thisGeneration, fishOnOverview).ToString() + " Gen: " + GA.generation.ToString();
        fishBarcode_Label.Text = fishOnOverview.GetChromosomeBarcode();
        fishGenes_Label.Text = fishOnOverview.GetGeneDescription();
    }
}
