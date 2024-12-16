using Godot;
using System;
using System.Collections.Generic;

public partial class FishGenerator : Polygon2D
{
    // This class generates the polygon for the fish. 

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        

    }

	public void GenerateFishPolygon() {
        // Use the nodes created in Fish.cs to build a polygon of the fish's body
        List<Node2D> nodes = new List<Node2D>(GetChildren().Count);

        foreach (Node child in GetChildren())
        {
            nodes.Add((Node2D)child);
        }

        Vector2[] points = new Vector2[]
        {
            nodes[0].Position + new Vector2(nodes[0].Scale.X, 0),
            nodes[0].Position + new Vector2(0, -nodes[0].Scale.Y),
            nodes[1].Position + new Vector2(0, -nodes[1].Scale.Y),
            nodes[2].Position + new Vector2(0, -nodes[2].Scale.Y),
            nodes[3].Position + new Vector2(0, -nodes[3].Scale.Y),
            nodes[3].Position + new Vector2(-nodes[3].Scale.X, 0),
            new Vector2(nodes[3].Position.X, 0),
            nodes[4].Position + new Vector2(-nodes[4].Scale.X, 0),
            nodes[4].Position + new Vector2(0, nodes[4].Scale.Y),
            nodes[2].Position + new Vector2(0, nodes[2].Scale.Y),
            nodes[1].Position + new Vector2(0, nodes[1].Scale.Y),
            nodes[0].Position + new Vector2(0, nodes[0].Scale.Y)
        };

        this.Polygon = null;
        this.Polygon = points;
    }

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}
}
