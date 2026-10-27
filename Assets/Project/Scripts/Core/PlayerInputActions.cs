using System;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Minimal code-defined Input System wrapper for the vertical slice.
/// Actions: Move, Look, Pick, Resonate, DebugToggle.
/// </summary>
public class PlayerInputActions : IDisposable
{
    public InputActionAsset Asset { get; }
    public InputActionMap Player { get; }

    public PlayerInputActions()
    {
        Asset = ScriptableObject.CreateInstance<InputActionAsset>();

        Player = new InputActionMap("Player");

        InputAction move = Player.AddAction("Move", InputActionType.Value);
        move.expectedControlType = "Vector2";

        InputAction look = Player.AddAction("Look", InputActionType.Value);
        look.expectedControlType = "Vector2";

        InputAction pick = Player.AddAction("Pick", InputActionType.Button);
        pick.expectedControlType = "Button";

        InputAction resonate = Player.AddAction("Resonate", InputActionType.Button);
        resonate.expectedControlType = "Button";

        InputAction debugToggle = Player.AddAction("DebugToggle", InputActionType.Button);
        debugToggle.expectedControlType = "Button";

        move.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w")
            .With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a")
            .With("Right", "<Keyboard>/d");
        move.AddBinding("<Gamepad>/leftStick");

        look.AddBinding("<Mouse>/delta");
        look.AddBinding("<Gamepad>/rightStick");

        pick.AddBinding("<Keyboard>/f");
        pick.AddBinding("<Gamepad>/buttonWest");

        resonate.AddBinding("<Mouse>/rightButton");
        resonate.AddBinding("<Gamepad>/leftTrigger");

        debugToggle.AddBinding("<Keyboard>/f1");

        Asset.AddActionMap(Player);
    }

    public void Enable() => Asset.Enable();

    public void Disable() => Asset.Disable();

    public void Dispose()
    {
        if (Asset != null)
            UnityEngine.Object.Destroy(Asset);
    }
}
