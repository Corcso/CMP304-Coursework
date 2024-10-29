using Godot;
using System;

public partial class Fish : Node2D
{

	// Fish Parameters
	[Export] byte length; // 0-255 (Remap 4-10)
    [Export] byte upperMuscle; // 0-255 (Remap 0.1-1.5)
    [Export] byte lowerMuscle; // 0-255 (Remap 0.1-1.5)
    [Export] byte tailHeight; // 0-255 (Remap 1-5)

	float lengthActual;
	float upperMuscleActual;
	float lowerMuscleActual;
	float tailHeightActual;

	float flapSpeed; // (3^strength)/tailHeight
	float dragConstant; // based on biggest muscle

	float timeAlive;
	float lastFlapAngle;

    // Called when the node enters the scene tree for the first time.
    public override void _Ready()
	{
        timeAlive = 0;
        lastFlapAngle = 0;

        LoadRandom();
        GrowFish();

    }

    public void LoadRandom() { 
        RandomNumberGenerator rng = new RandomNumberGenerator();

        LoadGene(rng.Randi());
    }

    /// <summary>
    /// Loads a gene from its binary footprint
    /// </summary>
    /// <param name="gene">32 bit gene</param>
    public void LoadGene(uint gene) {
        // Store attributes
        length = (byte)((gene & 0xFF000000) >> 24);
        upperMuscle = (byte)((gene & 0x00FF0000) >> 16);
        lowerMuscle = (byte)((gene & 0x0000FF00) >> 8);
        tailHeight = (byte)((gene & 0x000000FF) >> 0);
    }

    public void LoadValues(byte length, byte upperMuscle, byte lowerMuscle, byte tailHeight) {
        // Store attributes
        this.length = length;
        this.upperMuscle = upperMuscle;
        this.lowerMuscle = lowerMuscle;
        this.tailHeight = tailHeight;
    }

    /// <summary>
    /// Takes the values assigned to length, upper & lower muscle and tail height and turns them into float values and creates the polygon.
    /// Basically setting up the fish from the gene values
    /// </summary>
    private void GrowFish() { 
        // First remap genes
        lengthActual = ((float)length / 255) * (10 - 4) + 4;
        upperMuscleActual = ((float)upperMuscle / 255) * (1.5f - 0.1f) + 0.1f;
        lowerMuscleActual = ((float)lowerMuscle / 255) * (1.5f - 0.1f) + 0.1f;
        tailHeightActual = ((float)tailHeight / 255) * (5 - 1) + 1;

        // Next clalculate speed and drag
        float strength = (upperMuscleActual + lowerMuscleActual) / 4.0f;
        flapSpeed = Mathf.Pow(9, strength) / (tailHeightActual * tailHeightActual);
        dragConstant = Mathf.Pow(Mathf.Max(upperMuscleActual, lowerMuscleActual), 2);

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
        GD.Print(Name + " I am speed: " + flapSpeed.ToString() + " and power " + tailHeight.ToString());
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		timeAlive += (float)delta;

		float currentFlapAngle = Mathf.Sin(timeAlive * flapSpeed * 6) * tailHeight;
		float flapThisFrame = Mathf.Abs(currentFlapAngle - lastFlapAngle);

		float toMove = Mathf.Clamp((flapThisFrame * (float)delta * 50) - (dragConstant * 2.0f), 0, 100000);


        Position = new Vector2(Position.X + toMove, Position.Y) ;

		lastFlapAngle = currentFlapAngle;
	}
}
