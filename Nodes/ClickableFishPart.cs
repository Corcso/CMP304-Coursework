using Godot;
using System;

public partial class ClickableFishPart : Area2D
{
    // UI reference
    UI uiScript;

	// Called when the node enters the scene tree for the first time.
	public override void _Ready()
	{
        uiScript = GetTree().Root.GetNode<UI>("./Node2D/UI");
	}

	// Called every frame. 'delta' is the elapsed time since the previous frame.
	public override void _Process(double delta)
	{
	}

    public override void _InputEvent(Viewport viewport, InputEvent @event, int shapeIdx)
    {
        // If we clock on the fish collider. Open the fish overview for this fish. 
        base._InputEvent(viewport, @event, shapeIdx);

        if (@event is InputEventMouseButton mouseEvent)
        {
            uiScript.ShowFishOverview(GetParent<Fish>());
        }
    }
}
