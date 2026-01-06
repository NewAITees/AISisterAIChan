using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
using UniVRM10;

public class VRMController : MonoBehaviour
{
    [SerializeField]
    private Vrm10Instance aiVrmInstance;
    [SerializeField]
    private Vrm10Instance aiVrmInstance2;
    [SerializeField]
    private AnimationClip idleClip;
    [SerializeField]
    private bool playIdleOnStart = true;

    private readonly Dictionary<Vrm10Instance, PlayableGraph> idleGraphs = new Dictionary<Vrm10Instance, PlayableGraph>();

    private readonly Dictionary<string, ExpressionPreset> expressionMap = new Dictionary<string, ExpressionPreset>()
    {
        { "普通", ExpressionPreset.neutral },
        { "笑顔", ExpressionPreset.happy },
        { "驚き", ExpressionPreset.surprised },
        { "怒り", ExpressionPreset.angry },
        { "悲しい", ExpressionPreset.sad },
        { "恥ずかしい", ExpressionPreset.relaxed },
    };

    public void ApplyTalk(GhostCommand command)
    {
        ApplyExpression(command.characterId, command.expression);
    }

    private void Start()
    {
        if (!playIdleOnStart || idleClip == null)
            return;

        StartIdle(aiVrmInstance);
        StartIdle(aiVrmInstance2);
    }

    private void OnDestroy()
    {
        foreach (var graph in idleGraphs.Values)
        {
            if (graph.IsValid())
                graph.Destroy();
        }
        idleGraphs.Clear();
    }

    public void ApplyExpression(int characterId, string expressionName)
    {
        var target = GetTargetInstance(characterId);
        if (target == null || target.Runtime == null)
            return;

        ResetAllExpressions(target);

        if (!string.IsNullOrEmpty(expressionName) && expressionMap.TryGetValue(expressionName, out var preset))
        {
            var key = ExpressionKey.CreateFromPreset(preset);
            target.Runtime.Expression.SetWeight(key, 1.0f);
        }
        else
        {
            var key = ExpressionKey.CreateFromPreset(ExpressionPreset.neutral);
            target.Runtime.Expression.SetWeight(key, 1.0f);
        }
    }

    private Vrm10Instance GetTargetInstance(int characterId)
    {
        switch (characterId)
        {
            case 0:
                return aiVrmInstance;
            case 1:
                return aiVrmInstance2;
            default:
                return null;
        }
    }

    private void StartIdle(Vrm10Instance instance)
    {
        if (instance == null || idleGraphs.ContainsKey(instance))
            return;

        var animator = instance.GetComponent<Animator>();
        if (animator == null)
            return;

        var graph = PlayableGraph.Create("VRMIdleMotion");
        graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime);

        var output = AnimationPlayableOutput.Create(graph, "IdleOutput", animator);
        idleClip.wrapMode = WrapMode.Loop;
        var clipPlayable = AnimationClipPlayable.Create(graph, idleClip);
        output.SetSourcePlayable(clipPlayable);
        graph.Play();

        idleGraphs.Add(instance, graph);
    }

    private void ResetAllExpressions(Vrm10Instance instance)
    {
        foreach (ExpressionPreset preset in Enum.GetValues(typeof(ExpressionPreset)))
        {
            if (preset == ExpressionPreset.custom)
                continue;

            var key = ExpressionKey.CreateFromPreset(preset);
            instance.Runtime.Expression.SetWeight(key, 0f);
        }
    }
}
