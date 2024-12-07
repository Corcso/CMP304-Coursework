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
    Label lastLargestDiff_Label;

    Label squareGenerationToActualSize_Label;
    Label rockCount_Label;

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
        lastLargestDiff_Label = GetNode<Label>("./Currently Playing UI/Panel/Last Largest Difference");
        squareGenerationToActualSize_Label = GetNode<Label>("./Settings Menu/Panel/Actual Generation Size");
        fishNumber_Label = GetNode<Label>("./Single Fish View/Panel/Fish Name");
        fishBarcode_Label = GetNode<RichTextLabel>("./Single Fish View/Panel/Fish Chromosome");
        fishGenes_Label = GetNode<RichTextLabel>("./Single Fish View/Panel/Fish Genes");

        rockCount_Label = GetNode<Label>("./Settings Menu/World Settings/Rock Count");
        
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

        GetNode<OptionButton>("./Settings Menu/Panel/Fitness Function").ItemSelected += (long index) => {
            switch (index)
            {
                case 0: // Furthest Fish, Death Bad
                    GA.FitnessFunction = GA.FurthestAndRockKills;
                    GA.fitnessFunctionName = "FurthestAndRockKills";
                    break;
                case 1: // Furthest Fish
                    GA.FitnessFunction = GA.FurthestOnly;
                    GA.fitnessFunctionName = "FurthestOnly";
                    break;
                case 2: // Survival Only
                    GA.FitnessFunction = GA.SurviveOnly;
                    GA.fitnessFunctionName = "SurviveOnly";
                    break;
            };
        };

        GetNode<OptionButton>("./Settings Menu/Panel/Recombination Function").ItemSelected += (long index) => {
            switch (index) {
                case 0: // Single Point Crossover
                    GA.RecombinationFunction = GA.SinglePointCrossover;
                    GA.recombinationFunctionName = "SinglePointCrossover";
                    break;
                case 1: // Double Point Crossover
                    GA.RecombinationFunction = GA.DoublePointCrossover;
                    GA.recombinationFunctionName = "DoublePointCrossover";
                    break;
                case 2: // Chromosome Midpoint
                    GA.RecombinationFunction = GA.ChromosomeMidpoint;
                    GA.recombinationFunctionName = "ChromosomeMidpoint";
                    break;
                case 3: // Gene Midpoints
                    GA.RecombinationFunction = GA.GeneMidpoints;
                    GA.recombinationFunctionName = "GeneMidpoints";
                    break;
            };    
        };

        GetNode<OptionButton>("./Settings Menu/Panel/Mutation Function").ItemSelected += (long index) => {
            switch (index)
            {
                case 0: // Flip 1 Random Bit
                    GA.MutationFunction = GA.SingleBitFlip;
                    GA.mutationFunctionName = "SingleBitFlip";
                    break;
                case 1: // Flip 2 Random Bits
                    GA.MutationFunction = GA.DoubleBitFlip;
                    GA.mutationFunctionName = "DoubleBitFlip";
                    break;
                case 2: // Flip 4 Random Bits
                    GA.MutationFunction = GA.QuadBitFlip;
                    GA.mutationFunctionName = "QuadBitFlip";
                    break;
                case 3: // Flip 8 Random Bits
                    GA.MutationFunction = GA.OctBitFlip;
                    GA.mutationFunctionName = "OctBitFlip";
                    break;
                case 4: // Flip 16 Random Bits
                    GA.MutationFunction = GA.HexadecBitFlip;
                    GA.mutationFunctionName = "HexadecBitFlip";
                    break;
                case 5: // All Random Bits
                    GA.MutationFunction = GA.CompleteRandom;
                    GA.mutationFunctionName = "CompleteRandom";
                    break;
            };
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


        // World Settings
        GetNode<Range>("./Settings Menu/World Settings/Rock Radius").ValueChanged += (double value) => {
            GA.rockRadius = (int)value;
            rockCount_Label.Text = ((int)(GA.rockRadius * GA.rockDensity)).ToString() + " rocks will spawn.";
        };

        GetNode<Range>("./Settings Menu/World Settings/Rock Density").ValueChanged += (double value) => {
            GA.rockDensity = (float)value;
            rockCount_Label.Text = ((int)(GA.rockRadius * GA.rockDensity)).ToString() + " rocks will spawn.";
        };

        GetNode<Range>("./Settings Menu/World Settings/Drag Factor").ValueChanged += (double value) => {
            GA.dragFactor = (float)value;
        };

        GetNode<Range>("./Settings Menu/World Settings/Strength Multiplier").ValueChanged += (double value) => {
            GA.dragFactor = (float)value;
        };

        GetNode<Range>("./Settings Menu/Generation Cap Panel/Generation Cap").ValueChanged += (double value) => {
            GA.generationCap = (int)value;
        };

        GetNode<CheckBox>("./Settings Menu/Generation Cap Panel/Generation Cap Active").Toggled += (bool value) => {
            GA.generationCapActive = value;
        };

    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        currentTick_Label.Text = "Tick: " + GA.ticksThisGeneration.ToString();
        currentGeneration_Label.Text = "Gen: " + GA.generation.ToString();
        lastBestFitness_Label.Text = "Best Fitness: " + MathF.Round(GA.bestFitness, 3).ToString();
        lastLargestDiff_Label.Text = "Largest Diff.: " + GA.largestDifference.ToString();
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
