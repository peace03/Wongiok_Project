using UnityEngine;

public class GameInputReader : MonoBehaviour
{
    private GameInputAction _input;

    #region Player Input
    public Vector2 MoveInput => _input.Player.Move.ReadValue<Vector2>();
    public bool JumpTriggered => _input.Player.Jump.WasPressedThisFrame();
    public bool IsJumping => _input.Player.Jump.IsPressed();
    public bool AttackTriggered => _input.Player.Attack.WasPressedThisFrame();
    public bool DashTriggered => _input.Player.Dash.WasPressedThisFrame();
    public bool ParryTriggered => _input.Player.Parry.WasPressedThisFrame();
    public bool UseHealItemTriggered => _input.Player.UseHealItem.WasPressedThisFrame();
    #endregion

    #region UI Input
    public bool MenuPressed => _input.UI.ToggleMenu.WasPressedThisFrame();
    #endregion

    #region Skill Input
    public bool MagnumPressed => _input.Player.Magnum.WasPressedThisFrame();
    public bool RiflePressed => _input.Player.Rifle.WasPressedThisFrame();
    public bool SniperPressed => _input.Player.Sniper.WasPressedThisFrame();
    
    #endregion

    private void Awake()
    {
        _input = new GameInputAction();
    }

    private void OnEnable()
    {
        _input?.Enable();
    }

    private void OnDisable()
    {
        _input?.Disable();
    }

    private void OnDestroy()
    {
        _input?.Dispose();
    }
}
