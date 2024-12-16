using Godot;
using System;

public partial class CameraMovement : Camera2D
{
    Control UI;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        UI = GetNode<Control>("../UI");
	}

	const float SPEED = 400;

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
		// Super simple camera movement
		if (Input.IsKeyPressed(Key.W)) {
			Position = Position + new Vector2(0, -SPEED * (float)delta);
		}
        if (Input.IsKeyPressed(Key.S))
        {
            Position = Position + new Vector2(0, SPEED * (float)delta);
        }

        if (Input.IsKeyPressed(Key.A))
        {
            Position = Position + new Vector2(-SPEED * (float)delta, 0);
        }
        if (Input.IsKeyPressed(Key.D))
        {
            Position = Position + new Vector2(SPEED * (float)delta, 0);
        }

        // Set UI to follow camera
        UI.Position = Position + new Vector2(-576, -324);
    }
}
