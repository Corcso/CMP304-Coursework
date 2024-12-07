using Godot;
using System;

public partial class Fish : Node2D
{

    // Fish Parameters
    ulong myGene;
	[Export] byte length; // 0-255 (Remap 4-10)
    [Export] byte upperMuscle; // 0-255 (Remap 0.1-1.5)
    [Export] byte lowerMuscle; // 0-255 (Remap 0.1-1.5)
    [Export] byte tailHeight; // 0-255 (Remap 1-5)
    [Export] byte leftMovement; // 0-255 (Remap -0.15-0.15)
    [Export] byte rightMovement; // 0-255 (Remap -0.15-0.15)
    [Export] byte closeLeftMovement; // 0-255 (Remap -0.15-0.15)
    [Export] byte closeRightMovement; // 0-255 (Remap -0.15-0.15)

    float lengthActual;
	float upperMuscleActual;
	float lowerMuscleActual;
	float tailHeightActual;
	float leftMovementActual;
	float rightMovementActual;
	float closeLeftMovementActual;
	float closeRightMovementActual;

	float flapSpeed; // (3^strength)/tailHeight
	float dragConstant; // based on biggest muscle

	float timeAlive;
	float lastFlapAngle;

    RayCast2D leftEye;
    RayCast2D closeLeftEye;
    RayCast2D rightEye;
    RayCast2D closeRightEye;

    Label text;

    // Public for test
    public bool alive;

    // Reference to GA to check if paused and whatnot (Will be set by the GA on fish creation)
    public GeneticAlgorithm GA;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        timeAlive = 0;
        lastFlapAngle = 0;

        GD.Print("Ready!");
        

        leftEye = GetNode<RayCast2D>("Left Eye");
        closeLeftEye = GetNode<RayCast2D>("Close Left Eye");
        rightEye = GetNode<RayCast2D>("Right Eye");
        closeRightEye = GetNode<RayCast2D>("Close Right Eye");

        GetNode<Area2D>("./Death Zone").BodyEntered += (Node2D b) => {
            alive = false;
            Hide();
        };

        alive = true;
    }

    public void LoadRandom() { 
        RandomNumberGenerator rng = new RandomNumberGenerator();

        LoadGene((ulong)rng.Randi() << 32 | (ulong)rng.Randi());
    }

    /// <summary>
    /// Loads a gene from its binary footprint
    /// </summary>
    /// <param name="gene">32 bit gene</param>
    public void LoadGene(ulong gene) {
        // Store attributes
        closeLeftMovement =      (byte)((gene & 0xFF00000000000000) >> 56);
        closeRightMovement =      (byte)((gene & 0x00FF000000000000) >> 48);
        leftMovement =      (byte)((gene & 0x0000FF0000000000) >> 40);
        rightMovement =     (byte)((gene & 0x000000FF00000000) >> 32);
        length =            (byte)((gene & 0x00000000FF000000) >> 24);
        upperMuscle =       (byte)((gene & 0x0000000000FF0000) >> 16);
        lowerMuscle =       (byte)((gene & 0x000000000000FF00) >> 8);
        tailHeight =        (byte)((gene & 0x00000000000000FF) >> 0);

        text = GetNode<Label>("./Text");
        text.Text = (gene & 0xFFFFFFFFFFFFFFFF).ToString("X");

        myGene = gene;
        GrowFish();
    }

    public void Reset() {
        myGene = 0;
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

    public ulong GetGene() {
        return myGene;
    }

    /// <summary>
    /// Takes the values assigned to length, upper & lower muscle and tail height and turns them into float values and creates the polygon.
    /// Basically setting up the fish from the gene values
    /// </summary>
    private void GrowFish() { 
        // First remap genes
        closeLeftMovementActual = (float)leftMovement / 255 * 0.3f - 0.15f;
        closeRightMovementActual = (float)rightMovement / 255 * 0.3f - 0.15f;
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
        //if (close.IsColliding()) rightDistance = GlobalPosition.DistanceSquaredTo(rightEye.GetCollisionPoint()) / 1000.0f;
        ///if (rightEye.IsColliding()) rightDistance = GlobalPosition.DistanceSquaredTo(rightEye.GetCollisionPoint()) / 1000.0f;
        //if (rightEye.IsColliding()) GD.Print(GlobalPosition.DistanceSquaredTo(rightEye.GetCollisionPoint()) / 1000.0f);

        float turnThisFrame = (Mathf.Clamp((-rightDistance * rightMovementActual) + 0.0f, 0, 1) - Mathf.Clamp((-leftDistance * leftMovementActual) + 0.0f, 0, 1)) * (float)delta * 0.7f;

        Rotate(turnThisFrame);
        //GD.Print(turnThisFrame);

        

		float currentFlapAngle = Mathf.Sin(timeAlive + flapSpeed * 6) * tailHeight;
		float flapThisFrame = Mathf.Abs(currentFlapAngle - lastFlapAngle);

		float toMove = Mathf.Clamp((flapThisFrame * (float)delta * 50) - (dragConstant * 2.0f), 0, 100000);


        Position += toMove * GlobalTransform.BasisXform(new Vector2(1, 0));

		lastFlapAngle = currentFlapAngle;
        timeAlive += (float)delta;

    }
    
    private string[] geneColours = { "red", "green", "blue", "yellow", "purple", "white", "orange", "pink" };

    public string GetChromosomeBarcode() {
        string toReturn = "        ";
        // For each Gene
        for (int i = 16; i < 64; i += 8) { 
            byte gene = (byte)((myGene & (0xFF00000000000000 >> i)) >> (56-i));
            toReturn += "[color="+ geneColours[i/8]+"]";
            // For each pair of bytes
            for (int bp = 0; bp < 8; bp += 2) { 
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
            if (i == 24) toReturn += "\n";
            toReturn += "[/color]";
        }
        return toReturn;
    }

    public string GetGeneDescription() {
        return "[color=" + geneColours[4] + "]Length (4-10): " + MathF.Round(lengthActual, 3) +"[/color]\n" +
            "[color=" + geneColours[5] + "]Upper Muscle (0.1-1.5): " + MathF.Round(upperMuscleActual, 3) + "[/color]\n" +
            "[color=" + geneColours[6] + "]Lower Muscle (0.1-1.5): " + MathF.Round(lowerMuscleActual, 3) + "[/color]\n" +
            "[color=" + geneColours[7] + "]Tail Height (1-5): " + MathF.Round(tailHeightActual, 3) + "[/color]\n" +
            "[color=" + geneColours[3] + "]L. Turn Fac. (-0.15-0.15): " + MathF.Round(leftMovementActual, 3) + "[/color]\n" +
            "[color=" + geneColours[2] + "]R. Turn Fac. (-0.15-0.15): " + MathF.Round(rightMovementActual, 3) + "[/color]\n";
    }
}
