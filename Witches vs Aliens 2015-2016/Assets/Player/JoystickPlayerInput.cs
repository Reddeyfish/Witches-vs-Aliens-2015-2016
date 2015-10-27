﻿using UnityEngine;
using System.Collections;

public class JoystickPlayerInput : AbstractPlayerInput {

    protected override void updateAim()
    {
        action.aimingInput = new Vector2(Input.GetAxis(bindings.horizontalAimingAxisName), Input.GetAxis(bindings.verticalAimingAxisName));
    }

    protected override void checkAbilities()
    {
        if (Input.GetKeyDown(bindings.movementAbilityKey))
            action.FireAbility(AbilityType.MOVEMENT);
        if (Input.GetKeyDown(bindings.superAbilityKey))
            action.FireAbility(AbilityType.SUPER);

        if (Input.GetKeyUp(bindings.movementAbilityKey))
            action.StopFireAbility(AbilityType.MOVEMENT);
        if (Input.GetKeyUp(bindings.superAbilityKey))
            action.StopFireAbility(AbilityType.SUPER);
    }
}
