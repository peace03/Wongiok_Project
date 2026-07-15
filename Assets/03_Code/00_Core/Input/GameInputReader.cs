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
    public bool SkillAPressed => _input.Player.SkillA.WasPressedThisFrame();
    public bool SkillSPressed => _input.Player.SkillS.WasPressedThisFrame();
    public bool SkillDPressed => _input.Player.SkillD.WasPressedThisFrame();
    public bool SkillAReleased => _input.Player.SkillA.WasReleasedThisFrame();
    public bool SkillSReleased => _input.Player.SkillS.WasReleasedThisFrame();
    public bool SkillDReleased => _input.Player.SkillD.WasReleasedThisFrame();


    
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
