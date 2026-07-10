using UnityEngine;

public abstract class BossState
{
    protected BossController controller;
    protected IBossLogics logics;
    public BossState(BossController controller, IBossLogics logics)
    {
        this.controller = controller;
        this.logics = logics;
    }

    public virtual void Enter() { }
    public virtual void FixedUpdate() { }
    public abstract void Update();
    public virtual void Exit() { }
}
