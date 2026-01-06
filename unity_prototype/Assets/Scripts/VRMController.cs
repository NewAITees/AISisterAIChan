using System;
using System.Collections.Generic;
using UnityEngine;
using UniVRM10;

public class VRMController : MonoBehaviour
{
    [SerializeField]
    private Vrm10Instance aiVrmInstance;

    private readonly Dictionary<string, ExpressionPreset> expressionMap = new Dictionary<string, ExpressionPreset>()
    {
        { "普通", ExpressionPreset.Neutral },
        { "笑顔", ExpressionPreset.Happy },
        { "驚き", ExpressionPreset.Surprised },
        { "怒り", ExpressionPreset.Angry },
        { "悲しい", ExpressionPreset.Sad },
        { "恥ずかしい", ExpressionPreset.Relaxed },
    };

    public void ApplyTalk(GhostCommand command)
    {
        ApplyExpression(command.characterId, command.expression);
    }

    public void ApplyExpression(int characterId, string expressionName)
    {
        if (characterId != 0)
            return;

        if (aiVrmInstance == null || aiVrmInstance.Runtime == null)
            return;

        ResetAllExpressions(aiVrmInstance);

        if (!string.IsNullOrEmpty(expressionName) && expressionMap.TryGetValue(expressionName, out var preset))
        {
            var key = ExpressionKey.CreateFromPreset(preset);
            aiVrmInstance.Runtime.Expression.SetWeight(key, 1.0f);
        }
        else
        {
            var key = ExpressionKey.CreateFromPreset(ExpressionPreset.Neutral);
            aiVrmInstance.Runtime.Expression.SetWeight(key, 1.0f);
        }
    }

    private void ResetAllExpressions(Vrm10Instance instance)
    {
        foreach (ExpressionPreset preset in Enum.GetValues(typeof(ExpressionPreset)))
        {
            if (preset == ExpressionPreset.Unknown)
                continue;

            var key = ExpressionKey.CreateFromPreset(preset);
            instance.Runtime.Expression.SetWeight(key, 0f);
        }
    }
}
