using Godot;
using System;
using System.Collections.Generic;

public partial class GeneticAlgorithm : Node
{
    // Fish "prefab" "node scene" to spawn
	[Export] PackedScene FishTemplate;

    // Array of fish this generation, and fish to use to create the next generation. 
	public Fish[] thisGeneration;
    ulong[] toCreateNextGeneration; // Just chromosomes as thats all we need
	public int generation = 1; // The number of generation we are on. 

    // Random number generator, used throughout
	RandomNumberGenerator rng;

    // Square Generation Size parameter
	[Export] public int squareGenerationSize;
    // Mutation % parameter
	[Export] public float mutationPercentage;

    // Define fitness function in use and all the available ones
    public string fitnessFunctionName; // For statistical output
    public Func<Fish, float> FitnessFunction;
	public Func<Fish, float> FurthestAndRockKills;
	public Func<Fish, float> FurthestOnly;
	public Func<Fish, float> SurviveOnly;

    // Define recombination function in use and all the available ones
    public string recombinationFunctionName; // For statistical output
	public Func<ulong, ulong, ulong> RecombinationFunction;
	public Func<ulong, ulong, ulong> SinglePointCrossover;
	public Func<ulong, ulong, ulong> DoublePointCrossover;
	public Func<ulong, ulong, ulong> ChromosomeMidpoint;
	public Func<ulong, ulong, ulong> GeneMidpoints;

    // Define mutation function in use and all the available ones
    public string mutationFunctionName; // For statistical output
    public Func<ulong, ulong> MutationFunction;
    public Func<ulong, ulong> SingleBitFlip;
    public Func<ulong, ulong> DoubleBitFlip;
    public Func<ulong, ulong> QuadBitFlip;
    public Func<ulong, ulong> OctBitFlip;
    public Func<ulong, ulong> HexadecBitFlip;
    public Func<ulong, ulong> CompleteRandom;

    // Render every X ticks
    [Export] int renderEveryXTicks = 100;
    // Ticks per generation
    [Export] public int maxTicks = 5000;
    // Ticks so far this generation
	public int ticksThisGeneration = 0;

    // State for storing current simulation state
	public enum State { PRE_SIM, SIMULATING, PAUSED};
	State currentState;

	public float bestFitness; // Best fitness of the last generation
	public float avgFitness; // Average fitness of the last generation
	public int largestDifference; // Largest bit difference of the last generation. 
    // E.g. chromosome 0b1101 and 0b1001 have a difference of 1
    //      chromosome 0b1011 and 0b1100 have a difference of 3
    // Used to measure convergence

    // Rock Spawning
    [Export] PackedScene rockScene;
    public int rockRadius = 4000;
    public float rockDensity = 0.5f;

    // World fish paramaters
    public float dragFactor = 0.5f;
    public float strengthMultiplier = 1;

    // GA Simulation ID
    ulong simulationID;

    // Generation cap, a new simulation will automatically start after X generations simulated
    public bool generationCapActive = false;
    public int generationCap = 100;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
		//DisplayServer.WindowSetVsyncMode(DisplayServer.VSyncMode.Disabled);

        // Set physics ticks
		Engine.PhysicsTicksPerSecond = 60;

		
		rng = new RandomNumberGenerator();

		// Set current state
		currentState = State.PRE_SIM;

		// Define fitness function
		FurthestAndRockKills = (Fish toEvaluate) =>
		{
            // If the fish is dead return the smallest number
			if (!toEvaluate.alive) return float.MinValue;
            // Otherwise Return the distance of the fish 
            // + 1k / 2k to get most values between 0 and 1
            return (toEvaluate.Position.X + 1000.0f) / 2000.0f;
		};
        
        FurthestOnly = (Fish toEvaluate) =>
		{
            // Return the distance of the fish 
            // + 1k / 2k to get most values between 0 and 1
			return (toEvaluate.Position.X + 1000.0f) / 2000.0f;
		};

        SurviveOnly = (Fish toEvaluate) =>
        {
            // If the fish is dead return 0 otherwise 1
            if (!toEvaluate.alive) return 0;
            return 1;
        };

        FitnessFunction = FurthestAndRockKills;
        fitnessFunctionName = "FurthestAndRockKills";

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
        recombinationFunctionName = "SinglePointCrossover";

        GeneMidpoints = (ulong parent1, ulong parent2) => {
            // Get the midpoint of each gene, we can use (gene1 + gene2) / 2 as each gene is only 1 byte
            byte leftEyeAngle = (byte)((((parent1 & 0xFF00000000000000) >> 56) + ((parent2 & 0xFF00000000000000) >> 56)) / 2);
            byte rightEyeAngle = (byte)((((parent1 & 0x00FF000000000000) >> 48) + ((parent2 & 0x00FF000000000000) >> 48)) / 2);
            byte leftMovement =      (byte)((((parent1 & 0x0000FF0000000000) >> 40) + ((parent2 & 0x0000FF0000000000) >> 40)) / 2);
             byte rightMovement =     (byte)((((parent1 & 0x000000FF00000000) >> 32) + ((parent2 & 0x000000FF00000000) >> 32)) / 2);
             byte length =            (byte)((((parent1 & 0x00000000FF000000) >> 24) + ((parent2 & 0x00000000FF000000) >> 24)) / 2);
             byte upperMuscle =       (byte)((((parent1 & 0x0000000000FF0000) >> 16) + ((parent2 & 0x0000000000FF0000) >> 16)) / 2);
             byte lowerMuscle =       (byte)((((parent1 & 0x000000000000FF00) >> 8) +  ((parent2 & 0x000000000000FF00) >> 8)) / 2);
             byte tailHeight =        (byte)((((parent1 & 0x00000000000000FF) >> 0) +  ((parent2 & 0x00000000000000FF) >> 0)) / 2);

			// Combine these genes back into a chromosome and return
			ulong child = 0;
			child |= (ulong)leftEyeAngle << 56;
			child |= (ulong)rightEyeAngle << 48;
			child |= (ulong)leftMovement << 40;
			child |= (ulong)rightMovement << 32;
			child |= (ulong)length << 24;
			child |= (ulong)upperMuscle << 16;
			child |= (ulong)lowerMuscle << 8;
			child |= (ulong)tailHeight << 0;

			return child;
        };

        SingleBitFlip = (ulong unMutatedChromosome) =>
		{
			// Choose and XOR (flip) one bit
			int swapBit = rng.RandiRange(0, 63);
			ulong swapMask = (ulong)0x0000000000000001 << swapBit;

			return unMutatedChromosome ^ swapMask;
		};

        DoubleBitFlip = (ulong unMutatedChromosome) =>
        {
			// Same as single but for 2 bits
			ulong mutatedChromosome = unMutatedChromosome;
			for (int flip = 0; flip < 2; ++flip) {
				int swapBit = rng.RandiRange(0, 63);
				ulong swapMask = (ulong)0x0000000000000001 << swapBit;
				mutatedChromosome ^= swapMask;
            }

            return mutatedChromosome;
        };

        QuadBitFlip = (ulong unMutatedChromosome) =>
        {
            // Same as single but for 4 bits
            ulong mutatedChromosome = unMutatedChromosome;
            for (int flip = 0; flip < 4; ++flip)
            {
                int swapBit = rng.RandiRange(0, 63);
                ulong swapMask = (ulong)0x0000000000000001 << swapBit;
                mutatedChromosome ^= swapMask;
            }

            return mutatedChromosome;
        };

        OctBitFlip = (ulong unMutatedChromosome) =>
        {
            // Same as single but for 8 bits
            ulong mutatedChromosome = unMutatedChromosome;
            for (int flip = 0; flip < 8; ++flip)
            {
                int swapBit = rng.RandiRange(0, 63);
                ulong swapMask = (ulong)0x0000000000000001 << swapBit;
                mutatedChromosome ^= swapMask;
            }

            return mutatedChromosome;
        };

        HexadecBitFlip = (ulong unMutatedChromosome) =>
        {
            // Same as single but for 16 bits
            ulong mutatedChromosome = unMutatedChromosome;
            for (int flip = 0; flip < 16; ++flip)
            {
                int swapBit = rng.RandiRange(0, 63);
                ulong swapMask = (ulong)0x0000000000000001 << swapBit;
                mutatedChromosome ^= swapMask;
            }

            return mutatedChromosome;
        };

        CompleteRandom = (ulong unMutatedChromosome) =>
        {
            // Return a fresh completely random chromosome, ignoring the input
            return (ulong)rng.Randi() << 32 | (ulong)rng.Randi();
        };

        // Set the default
        MutationFunction = SingleBitFlip;
        mutationFunctionName = "SingleBitFlip";
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        // Only do stuff if we are simulating
		if(currentState != State.SIMULATING) return;
        // If this generation is over
		if (ticksThisGeneration > maxTicks) {
            // Spawn the new generation
            NewGeneration();
            // Reset tick counter
            ticksThisGeneration = 0;

            // If generation cap is active, and we have just done the last generation. Restart the entire simulation. 
            if (generationCapActive && generation >= generationCap)
            {
                End();
                Start();
                return;
            }

            // Increase the generation counter
            generation++;
		}

        // If its time to render, render.
		if(ticksThisGeneration % renderEveryXTicks == 0) { RenderingServer.RenderLoopEnabled = true; }
		if(ticksThisGeneration % renderEveryXTicks == 1) { RenderingServer.RenderLoopEnabled = false; }

        // Increase ticks this generation.
        ticksThisGeneration++;
	}

	
    /// <summary>
    /// Select the top fish for the reproduction of the next generation 
    /// </summary>
	public void PickEvolvedFish() {
		// Sort the array using the inbuilt sort algorithm.
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

        // Calculate last generations largest difference, higher numbers random pool, lower numbers converged pool
        largestDifference = 0;
        for (int i = 0; i < squareGenerationSize * squareGenerationSize; ++i)
        {
            for (int j = 0; j < squareGenerationSize * squareGenerationSize; ++j)
            {
                // Compare all bits of the fish's gene
                int thisDifference = 0;
                for (int b = 0; b < 64; ++b)
                {
                    ulong mask = (ulong)0x1 << b;
                    if ((mask & (thisGeneration[i].GetChromosome() ^ thisGeneration[j].GetChromosome())) > 0) thisDifference++;
                }
                // If this fish has a larger difference it is the largest difference so far. 
                if (thisDifference > largestDifference)
                {
                    largestDifference = thisDifference;
                }
            }
        }

        // Grab the top fish for the next generation
        for (int i = 0; i < squareGenerationSize; i++)
        {
			toCreateNextGeneration[i] = thisGeneration[i + (squareGenerationSize * squareGenerationSize) - squareGenerationSize].GetChromosome();
        }

        // Reset all fish and set them to not alive. 
        for (int i = 0; i < squareGenerationSize * squareGenerationSize; i++)
        {
			thisGeneration[i].alive = false;
			thisGeneration[i].Reset();
        }
    }

    /// <summary>
    /// Create and load the next set of chromosomes. 
    /// </summary>
	public void CreateNewPopulation()
	{
        // For every possible pair of parents
		for (int i = 0; i < squareGenerationSize; i++)
		{
            for (int j = 0; j < squareGenerationSize; j++)
            {
                // If the same fish, then just load this fish into the next pool, this means the chosen repopulation fish from last generation go into the next. 
				if (i == j)
				{
					thisGeneration[i * squareGenerationSize + j].LoadChromosome(toCreateNextGeneration[i]);
				}
                // Otherwise we need to recombine these 2 parents, and possibly mutate
				else {
                    // Use the recombination function to recombine the 2 fish.
					ulong childChromosome = RecombinationFunction(toCreateNextGeneration[i], toCreateNextGeneration[j]);
					// If we need to mutate, use the mutation function before setting the chromosome. Otherwise just use the recombined chromosome. 
                    float mutationRoll = rng.Randf();
                    if(mutationRoll < mutationPercentage) thisGeneration[i * squareGenerationSize + j].LoadChromosome(MutationFunction(childChromosome));
                    else thisGeneration[i * squareGenerationSize + j].LoadChromosome(childChromosome);
                    //RecombinationFunction(toCreateNextGeneration[i], toCreateNextGeneration[j], thisGeneration[i * 20 + j]);
                }
			}
        }
    }


    /// <summary>
    /// Summon the new fish in the game world with correct parameters. 
    /// </summary>
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

    /// <summary>
    /// Call all functions required when a new generation is needed. 
    /// </summary>
	public void NewGeneration() {
		PickEvolvedFish();

        CreateNewPopulation();

		SpawnNewPopulation();

        SummonNewRocks();

        OutputStatistics();
    }

    /// <summary>
    /// Start the simulation.
    /// </summary>
	public void Start()
	{
        // Set state
        currentState = State.SIMULATING;

        // Create arrays
        thisGeneration = new Fish[squareGenerationSize * squareGenerationSize];
        toCreateNextGeneration = new ulong[squareGenerationSize];

        // Create a random simulation ID for output
        simulationID = (ulong)rng.Randi() << 32 | (ulong)rng.Randi();

        // Summon initial generation
        for (int i = 0; i < squareGenerationSize * squareGenerationSize; i++)
        {
            thisGeneration[i] = FishTemplate.Instantiate<Fish>();
            thisGeneration[i].GA = this;
            thisGeneration[i].LoadRandom();
            thisGeneration[i].Position = new Vector2(-500, rng.RandiRange(-300, 300));
            GetNode<Node>("../Fish Tank").AddChild(thisGeneration[i]);
        }

        // Set generation to 0, ticks to 0 and summon rocks. 
        generation = 0;
        ticksThisGeneration = 0;
        SummonNewRocks();


    }

    /// <summary>
    /// Pause the simulation
    /// </summary>
	public void Pause() 
	{
		currentState = State.PAUSED;
        // Turn rendering back on 
        RenderingServer.RenderLoopEnabled = true;
    }

    /// <summary>
    /// Resume the simulation
    /// </summary>
    public void Unpause()
    {
        currentState = State.SIMULATING;
        // Turn rendering back on 
        RenderingServer.RenderLoopEnabled = false;
    }

    /// <summary>
    /// End the simulation and go back to the pre simulation settings. 
    /// </summary>
    public void End()
    {
        currentState = State.PRE_SIM;

        // Delete current generation
        for (int i = 0; i < squareGenerationSize * squareGenerationSize; i++)
        {
			thisGeneration[i].QueueFree();
			thisGeneration[i] = null;
        }
        // Turn rendering back on 
        RenderingServer.RenderLoopEnabled = true;

        
    }

	public State GetCurrentState() {
		return currentState;
	}

    // Function called at the end of each generation to output statistics to a file.
    private void OutputStatistics() {
        FileAccess file = null;
        // Wait until file is free, allows multithread access. Please note if there is critical errors the program may hang here. 
        while (file == null || !file.IsOpen())
        {
            if (!FileAccess.FileExists("res://./run.csv"))
            {
                file = FileAccess.Open("res://./run.csv", FileAccess.ModeFlags.Write);
                // If first write, add column headers
                file.StoreLine("Simulation ID,Generation,Best Fitness,Average Fitness,Largest Chromosome Difference,Mutation Rate,Mutation Function,Recombination Function,Fitness Function,Generatation Size,Generation Length,Rock Radius,Rock Density,Drag Factor,Strength Multiplier");
            }
            else file = FileAccess.Open("res://./run.csv", FileAccess.ModeFlags.ReadWrite);
        }
        // Go to the end and add this generation as a line. 
        file.SeekEnd(0);
        file.StoreLine(
            simulationID.ToString("X") + "," +
            generation.ToString() + "," + bestFitness.ToString() + "," + avgFitness.ToString() + "," + largestDifference.ToString() + "," +
            mutationPercentage.ToString() + "," +mutationFunctionName + "," + recombinationFunctionName + "," + fitnessFunctionName + "," + squareGenerationSize * squareGenerationSize + "," + maxTicks.ToString() + "," +
            rockRadius.ToString() + "," + rockDensity.ToString() + "," + dragFactor.ToString() + "," + strengthMultiplier.ToString() );
        // Close the file
        file.Close();
    }
    /// <summary>
    /// Summon a new random set of rocks
    /// </summary>
    private void SummonNewRocks() {
        // Delete all the old ones
        foreach (Node rock in GetNode<Node>("../Rocks").GetChildren())
        {
            rock.Free();
        }
        // For the number of rocks to be created. 
        for (int i = 0; i < rockDensity * rockRadius; ++i) {
            // Create a new rock.
            Node2D newRock = rockScene.Instantiate<Node2D>();
            GetNode<Node>("../Rocks").AddChild(newRock);
            // Generate random position for rock outside the safe zone
            while (Mathf.Abs(newRock.Position.X) < 500 && Mathf.Abs(newRock.Position.Y) < 500)
            {
                newRock.Position = new Vector2(rng.RandiRange(-rockRadius, rockRadius), rng.RandiRange(-rockRadius, rockRadius));
                newRock.RotationDegrees = rng.RandiRange(0, 360);
            }
        }
    }

}
