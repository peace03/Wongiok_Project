using UnityEngine;

public abstract class BossState
{
    private BossController controller;
    public BossState(BossController controller)
    {
        this.controller = controller;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void Exit() { }
}
