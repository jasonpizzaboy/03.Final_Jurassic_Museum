using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    
    public string moveHorizontalAxisName = "Horizontal";
    public string moveVerticalAxisName = "Vertical";

    public string fireButtonName = "Fire1";
    public string jumpButtonName = "Jump";
    public string reloadButtonName = "Reload";


    public Vector2 moveInput { get; private set; }
    public bool fire { get; set; }
    public bool reload { get; set; }
    public bool jump { get; set; }
    
   public void Move(Vector2 InputDirection)
    {
        moveInput = InputDirection;
        if (moveInput.sqrMagnitude > 1) moveInput = moveInput.normalized;
    }

    public void Touch(int num)
    {
        switch (num)
        {
            case 1:
            fire = true;
            break;

            case 2:
            jump = true;
            break;

            case 3:
            reload = true;
            break;

        }
    }

    private void Update()
    {
        if (GameManager.Instance != null
            && GameManager.Instance.isGameover)
        {
            moveInput = Vector2.zero;
            fire = false;
            reload = false;
            jump = false;
            return;
        }
/*
        jump = Input.GetButtonDown(jumpButtonName);
        fire = Input.GetButton(fireButtonName);
        reload = Input.GetButtonDown(reloadButtonName);
        */
    }
}