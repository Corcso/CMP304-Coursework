using Godot;
using System;
using System.Collections.Generic;

public partial class FishGenerator : Polygon2D
{


	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        

    }

	public void GenerateFishPolygon() {
        List<Node2D> spheres = new List<Node2D>(GetChildren().Count);

        foreach (Node child in GetChildren())
        {
            spheres.Add((Node2D)child);
        }

        Vector2[] points = new Vector2[]
        {
            spheres[0].Position + new Vector2(spheres[0].Scale.X, 0),
            spheres[0].Position + new Vector2(0, -spheres[0].Scale.Y),
            spheres[1].Position + new Vector2(0, -spheres[1].Scale.Y),
            spheres[2].Position + new Vector2(0, -spheres[2].Scale.Y),
            spheres[3].Position + new Vector2(0, -spheres[3].Scale.Y),
            spheres[3].Position + new Vector2(-spheres[3].Scale.X, 0),
            new Vector2(spheres[3].Position.X, 0),
            spheres[4].Position + new Vector2(-spheres[4].Scale.X, 0),
            spheres[4].Position + new Vector2(0, spheres[4].Scale.Y),
            spheres[2].Position + new Vector2(0, spheres[2].Scale.Y),
            spheres[1].Position + new Vector2(0, spheres[1].Scale.Y),
            spheres[0].Position + new Vector2(0, spheres[0].Scale.Y)
        };

        Polygon = points;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
