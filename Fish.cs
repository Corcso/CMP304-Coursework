using Godot;
using System;

public partial class Fish : Node2D
{

    // Fish Parameters
    ulong myChromosome;
	[Export] byte length; // 0-255 (Remap 4-10)
    [Export] byte upperMuscle; // 0-255 (Remap 0.1-1.5)
    [Export] byte lowerMuscle; // 0-255 (Remap 0.1-1.5)
    [Export] byte tailHeight; // 0-255 (Remap 1-5)
    [Export] byte leftMovement; // 0-255 (Remap -0.15-0.15)
    [Export] byte rightMovement; // 0-255 (Remap -0.15-0.15)
    [Export] byte leftEyeAngle; // 0-255 (Remap 0-90)
    [Export] byte rightEyeAngle; // 0-255 (Remap 0-90)

    // Fish paramaters in the ranges above
    float lengthActual;
	float upperMuscleActual;
	float lowerMuscleActual;
	float tailHeightActual;
	float leftMovementActual;
	float rightMovementActual;
	float leftEyeAngleActual;
	float rightEyeAngleActual;

	float flapSpeed; // (3^strength)/tailHeight
	float dragConstant; // Based on biggest muscle, constant force applied against the fish.

	float timeAlive; // Time the fish was alive for
	float lastFlapAngle; // Last flap angle of the sin wave the fish was at. 

    // Left and right eye raycasts
    RayCast2D leftEye;
    RayCast2D rightEye;

    // Is Alive boolean
    public bool alive;

    // Reference to GA to check if paused and whatnot (Will be set by the GA on fish creation)
    public GeneticAlgorithm GA;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        // Set defaults and get any relevant nodes
        timeAlive = 0;
        lastFlapAngle = 0;

        leftEye = GetNode<RayCast2D>("Left Eye");
        rightEye = GetNode<RayCast2D>("Right Eye");

        // If something enters our death zone "kill" the fish. Fish cannot collide with eachother, only rocks.
        GetNode<Area2D>("./Death Zone").BodyEntered += (Node2D b) => {
            alive = false;
            Hide();
        };

        alive = true;
    }

    /// <summary>
    /// Load a random chromosome into the fish
    /// </summary>
    public void LoadRandom() { 
        RandomNumberGenerator rng = new RandomNumberGenerator();

        LoadChromosome((ulong)rng.Randi() << 32 | (ulong)rng.Randi());
    }

    /// <summary>
    /// Loads a chromosome from its binary format
    /// </summary>
    /// <param name="chromosome">64 bit chromosome</param>
    public void LoadChromosome(ulong chromosome) {
        // Store attributes
        rightEyeAngle =      (byte)((chromosome & 0xFF00000000000000) >> 56);
        leftEyeAngle =      (byte)((chromosome & 0x00FF000000000000) >> 48);
        leftMovement =      (byte)((chromosome & 0x0000FF0000000000) >> 40);
        rightMovement =     (byte)((chromosome & 0x000000FF00000000) >> 32);
        length =            (byte)((chromosome & 0x00000000FF000000) >> 24);
        upperMuscle =       (byte)((chromosome & 0x0000000000FF0000) >> 16);
        lowerMuscle =       (byte)((chromosome & 0x000000000000FF00) >> 8);
        tailHeight =        (byte)((chromosome & 0x00000000000000FF) >> 0);

        myChromosome = chromosome;
        GrowFish();
    }

    public void Reset() {
        myChromosome = 0;
        leftMovement = 0;
        rightMovement = 0;
        length = 0;
        upperMuscle = 0;
        lowerMuscle = 0;
        tailHeight = 0;
        timeAlive = 0;
        lastFlapAngle = 0;
        foreach (Node bodySphere in GetChild<Node>(0).GetChildren()) { 
            bodySphere.Free();
        }
        Rotation = 0;
    }

    /// <summary>
    /// Get the chromosome from the fish
    /// </summary>
    /// <returns>Binary Chromosome</returns>
    public ulong GetChromosome() {
        return myChromosome;
    }

    /// <summary>
    /// Takes the values assigned to length, upper & lower muscle and tail height and turns them into float values and creates the polygon.
    /// Basically setting up the fish from the gene values
    /// </summary>
    private void GrowFish() { 
        // First remap genes
        leftEyeAngleActual = (float)leftEyeAngle / 255 * 90.0f;
        rightEyeAngleActual = (float)rightEyeAngle / 255 * 90.0f;
        leftMovementActual = (float)leftMovement / 255 * 0.3f - 0.15f;
        rightMovementActual = (float)rightMovement / 255 * 0.3f - 0.15f;
        lengthActual = ((float)length / 255) * (10 - 4) + 4;
        upperMuscleActual = ((float)upperMuscle / 255) * (1.5f - 0.1f) + 0.1f;
        lowerMuscleActual = ((float)lowerMuscle / 255) * (1.5f - 0.1f) + 0.1f;
        tailHeightActual = ((float)tailHeight / 255) * (5 - 1) + 1;

        // Next clalculate speed and drag
        float strength = (upperMuscleActual + lowerMuscleActual) / 4.0f * GA.strengthMultiplier;
        flapSpeed = Mathf.Pow(9, strength) / (tailHeightActual * tailHeightActual);
        dragConstant = Mathf.Pow(Mathf.Max(upperMuscleActual, lowerMuscleActual), 2) * GA.dragFactor;

        // Create Polygon points
        Node2D head = new Node2D();
        head.Position = new Vector2(lengthActual * 1, 0);
        head.Scale = new Vector2(0.2f, 0.2f);
        GetChild<Node>(0).AddChild(head);

        Node2D middleMuscle = new Node2D();
        middleMuscle.Position = new Vector2(lengthActual * 0.5f, 0);
        middleMuscle.Scale = new Vector2(upperMuscleActual, upperMuscleActual);
        GetChild<Node>(0).AddChild(middleMuscle);

        Node2D backMuscle = new Node2D();
        backMuscle.Position = new Vector2(0, 0);
        backMuscle.Scale = new Vector2(lowerMuscleActual, lowerMuscleActual);
        GetChild<Node>(0).AddChild(backMuscle);

        Node2D tailTop = new Node2D();
        tailTop.Position = new Vector2(-1, -tailHeightActual);
        tailTop.Scale = new Vector2(0.2f, 0.2f);
        GetChild<Node>(0).AddChild(tailTop);

        Node2D tailBottom = new Node2D();
        tailBottom.Position = new Vector2(-1, tailHeightActual);
        tailBottom.Scale = new Vector2(0.2f, 0.2f);
        GetChild<Node>(0).AddChild(tailBottom);

        // Create the polygon
        GetChild<FishGenerator>(0).GenerateFishPolygon();

        // Set raycasts for eye angles
        leftEye = GetNode<RayCast2D>("Left Eye");
        rightEye = GetNode<RayCast2D>("Right Eye");
        leftEye.TargetPosition = new Vector2(Mathf.Sin(Mathf.DegToRad(leftEyeAngleActual)), -Mathf.Cos(Mathf.DegToRad(leftEyeAngleActual))) * 250;
        rightEye.TargetPosition = new Vector2(Mathf.Sin(Mathf.DegToRad(rightEyeAngleActual)), Mathf.Cos(Mathf.DegToRad(rightEyeAngleActual))) * 250;

        // Debug output
        //GD.Print(Name + " I am speed: " + flapSpeed.ToString() + " and power " + tailHeight.ToString() + "L" + leftMovementActual.ToString() + "R" + rightMovementActual.ToString());
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
        // If dead dont move
        if (!alive) return;
        // If paused also dont move. 
        if (GA.GetCurrentState() == GeneticAlgorithm.State.PAUSED) return;
        delta = 1.0f / Engine.PhysicsTicksPerSecond;

        // Process turning
        float leftDistance = 1000;
        float rightDistance = 1000;
        if (leftEye.IsColliding()) leftDistance = GlobalPosition.DistanceSquaredTo(leftEye.GetCollisionPoint()) / 1000.0f;
        if (rightEye.IsColliding()) rightDistance = GlobalPosition.DistanceSquaredTo(rightEye.GetCollisionPoint()) / 1000.0f;

        // Turn amount formula
        float turnThisFrame = (Mathf.Clamp((rightDistance * rightMovementActual), -1, 1) - Mathf.Clamp((leftDistance * leftMovementActual), -1, 1)) * (float)delta * 0.7f;

        Rotate(turnThisFrame);

        
        //Process movement

        // Get the angle of the tail this frame
		float currentFlapAngle = Mathf.Sin(timeAlive + flapSpeed * 6) * tailHeight;
        // Compare the difference between this frame and last to get angle difference, e.g amount flapped. 
		float flapThisFrame = Mathf.Abs(currentFlapAngle - lastFlapAngle);

        // Convert this into a distance to move. 
		float toMove = Mathf.Clamp((flapThisFrame * (float)delta * 50) - (dragConstant * 2.0f), 0, 100000);

        // Move the fish, this distance forward. 
        Position += toMove * GlobalTransform.BasisXform(new Vector2(1, 0));

        // Set last flap angle to the one calculated this frame
		lastFlapAngle = currentFlapAngle;

        // Add onto time alive
        timeAlive += (float)delta;

    }
    

    // Colours for each gene in the fish view menu
    private string[] geneColours = { "red", "green", "blue", "yellow", "purple", "white", "orange", "pink" };

    /// <summary>
    /// Returns the Chromosome Barcode for use in fish view screen.
    /// </summary>
    /// <returns>Chromosome Barcode String</returns>
    public string GetChromosomeBarcode() {
        string toReturn = "";
        // For each Gene
        for (int i = 0; i < 64; i += 8) { 
            byte gene = (byte)((myChromosome & (0xFF00000000000000 >> i)) >> (56-i));
            toReturn += "[color="+ geneColours[i/8]+"]";
            // For each pair of bytes
            for (int bp = 0; bp < 8; bp += 2) { 
            // Switch over 00 01 10 or 11
            switch ((byte)((gene & (0b11000000 >> bp)) >> (6 - bp)))
                {
                    case 0b00:
                        toReturn += " ";
                        break;
                    case 0b01:
                        toReturn += "▐";
                        break;
                    case 0b10:
                        toReturn += "▌";
                        break;
                    case 0b11:
                        toReturn += "█";
                        break;
                }
            }
            // If halfway, newline
            if (i == 24) toReturn += "\n";
            toReturn += "[/color]";
        }
        return toReturn;
    }

    /// <summary>
    /// Returns the Gene description from this fish's chromosome. Uses the same colours as the barcode. 
    /// </summary>
    /// <returns>Gene description</returns>
    public string GetGeneDescription() {
        return "[color=" + geneColours[4] + "]Length (4-10): " + MathF.Round(lengthActual, 3) +"[/color]\n" +
            "[color=" + geneColours[5] + "]Upper Muscle (0.1-1.5): " + MathF.Round(upperMuscleActual, 3) + "[/color]\n" +
            "[color=" + geneColours[6] + "]Lower Muscle (0.1-1.5): " + MathF.Round(lowerMuscleActual, 3) + "[/color]\n" +
            "[color=" + geneColours[7] + "]Tail Height (1-5): " + MathF.Round(tailHeightActual, 3) + "[/color]\n" +
            "[color=" + geneColours[3] + "]L. Turn Fac. (-0.15-0.15): " + MathF.Round(leftMovementActual, 3) + "[/color]\n" +
            "[color=" + geneColours[2] + "]R. Turn Fac. (-0.15-0.15): " + MathF.Round(rightMovementActual, 3) + "[/color]\n" +
            "[color=" + geneColours[1] + "]R. Eye Angle (0-90): " + MathF.Round(leftEyeAngleActual, 3) + "[/color]\n" +
            "[color=" + geneColours[0] + "]L. Eye Angle (0-90): " + MathF.Round(rightEyeAngleActual, 3) + "[/color]\n";
    }
}
