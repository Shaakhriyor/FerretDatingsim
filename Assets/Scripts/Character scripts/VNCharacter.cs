using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VNCharacter : MonoBehaviour
{
    [Serializable]
    public class Expression
    {
        public string expressionName;
        public GameObject expressionObject;
    }

    [Serializable]
    public class MovablePart
    {
        [Tooltip("A unique name for this body part, for example LeftUpperArm, Head, Tail, LeftHand.")]
        public string partId;

        public Transform target;
    }

    [Serializable]
    public class SavedPartPose
    {
        public string partId;

        public Vector3 localPosition;
        public Vector3 localEulerAngles;
        public Vector3 localScale;
    }

    [Serializable]
    public class CharacterPose
    {
        public string poseName = "Neutral";

        public List<SavedPartPose> parts = new();
    }

    [Header("Character")]
    [SerializeField] private string characterName = "Character";

    [Header("Expressions")]
    [Tooltip("Add every face/expression this specific character has.")]
    [SerializeField] private List<Expression> expressions = new();

    [Header("Movable Parts")]
    [Tooltip("Add ANY movable parts this character has. Different characters can have completely different lists.")]
    [SerializeField] private List<MovablePart> movableParts = new();

    [Header("Saved Poses")]
    [SerializeField] private List<CharacterPose> poses = new();

    [Header("Pose Animation")]
    [SerializeField] private float poseTransitionTime = 0.35f;

    [Header("Pose Creator")]
    [SerializeField] private string poseNameToCapture = "Neutral";

    [Header("Testing")]
    [SerializeField] private string testExpression = "Thinking";
    [SerializeField] private string testPose = "Neutral";

    private Coroutine poseCoroutine;

    public string CharacterName => characterName;

    // =========================================================
    // EXPRESSIONS
    // =========================================================

    public void SetExpression(string expressionName)
    {
        if (string.IsNullOrWhiteSpace(expressionName))
            return;

        Expression wantedExpression = null;

        foreach (Expression expression in expressions)
        {
            if (expression == null)
                continue;

            if (string.Equals(
                expression.expressionName,
                expressionName,
                StringComparison.OrdinalIgnoreCase))
            {
                wantedExpression = expression;
                break;
            }
        }

        if (wantedExpression == null)
        {
            Debug.LogWarning(
                $"{characterName} does not have an expression called '{expressionName}'.",
                this);

            return;
        }

        foreach (Expression expression in expressions)
        {
            if (expression == null || expression.expressionObject == null)
                continue;

            expression.expressionObject.SetActive(
                expression == wantedExpression);
        }

        Debug.Log(
            $"{characterName} expression changed to '{expressionName}'.",
            this);
    }

    // =========================================================
    // POSES
    // =========================================================

    public void SetPose(string poseName)
    {
        CharacterPose pose = FindPose(poseName);

        if (pose == null)
        {
            Debug.LogWarning(
                $"{characterName} does not have a pose called '{poseName}'.",
                this);

            return;
        }

        // In Edit Mode coroutines cannot animate normally,
        // so just apply the pose instantly.
        if (!Application.isPlaying)
        {
            ApplyPoseInstantly(pose);
            return;
        }

        if (poseCoroutine != null)
        {
            StopCoroutine(poseCoroutine);
        }

        poseCoroutine = StartCoroutine(
            AnimateToPose(pose));
    }

    public void SetPoseInstant(string poseName)
    {
        CharacterPose pose = FindPose(poseName);

        if (pose == null)
        {
            Debug.LogWarning(
                $"{characterName} does not have a pose called '{poseName}'.",
                this);

            return;
        }

        if (poseCoroutine != null)
        {
            StopCoroutine(poseCoroutine);
            poseCoroutine = null;
        }

        ApplyPoseInstantly(pose);
    }

    private CharacterPose FindPose(string poseName)
    {
        foreach (CharacterPose pose in poses)
        {
            if (pose == null)
                continue;

            if (string.Equals(
                pose.poseName,
                poseName,
                StringComparison.OrdinalIgnoreCase))
            {
                return pose;
            }
        }

        return null;
    }

    private IEnumerator AnimateToPose(CharacterPose pose)
    {
        Dictionary<string, Vector3> startPositions = new();
        Dictionary<string, Quaternion> startRotations = new();
        Dictionary<string, Vector3> startScales = new();

        foreach (MovablePart part in movableParts)
        {
            if (part == null ||
                part.target == null ||
                string.IsNullOrWhiteSpace(part.partId))
            {
                continue;
            }

            startPositions[part.partId] =
                part.target.localPosition;

            startRotations[part.partId] =
                part.target.localRotation;

            startScales[part.partId] =
                part.target.localScale;
        }

        float elapsed = 0f;
        float duration =
            Mathf.Max(0.01f, poseTransitionTime);

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t =
                Mathf.Clamp01(elapsed / duration);

            // Smoothstep
            t = t * t * (3f - 2f * t);

            foreach (SavedPartPose savedPart in pose.parts)
            {
                MovablePart livePart =
                    FindMovablePart(savedPart.partId);

                if (livePart == null ||
                    livePart.target == null)
                {
                    continue;
                }

                if (!startPositions.ContainsKey(savedPart.partId))
                    continue;

                livePart.target.localPosition =
                    Vector3.Lerp(
                        startPositions[savedPart.partId],
                        savedPart.localPosition,
                        t);

                livePart.target.localRotation =
                    Quaternion.Slerp(
                        startRotations[savedPart.partId],
                        Quaternion.Euler(savedPart.localEulerAngles),
                        t);

                livePart.target.localScale =
                    Vector3.Lerp(
                        startScales[savedPart.partId],
                        savedPart.localScale,
                        t);
            }

            yield return null;
        }

        ApplyPoseInstantly(pose);

        poseCoroutine = null;
    }

    private void ApplyPoseInstantly(CharacterPose pose)
    {
        foreach (SavedPartPose savedPart in pose.parts)
        {
            MovablePart livePart =
                FindMovablePart(savedPart.partId);

            if (livePart == null ||
                livePart.target == null)
            {
                continue;
            }

            livePart.target.localPosition =
                savedPart.localPosition;

            livePart.target.localRotation =
                Quaternion.Euler(
                    savedPart.localEulerAngles);

            livePart.target.localScale =
                savedPart.localScale;
        }
    }

    private MovablePart FindMovablePart(string partId)
    {
        foreach (MovablePart part in movableParts)
        {
            if (part == null)
                continue;

            if (string.Equals(
                part.partId,
                partId,
                StringComparison.OrdinalIgnoreCase))
            {
                return part;
            }
        }

        return null;
    }

    // =========================================================
    // POSE CAPTURE
    // =========================================================

    [ContextMenu("Capture Current Pose")]
    private void CaptureCurrentPose()
    {
        if (string.IsNullOrWhiteSpace(poseNameToCapture))
        {
            Debug.LogWarning(
                "Enter a pose name in Pose Name To Capture first.",
                this);

            return;
        }

        CharacterPose pose =
            FindPose(poseNameToCapture);

        if (pose == null)
        {
            pose = new CharacterPose();
            pose.poseName = poseNameToCapture;
            poses.Add(pose);
        }

        pose.parts.Clear();

        HashSet<string> usedIds =
            new HashSet<string>(
                StringComparer.OrdinalIgnoreCase);

        foreach (MovablePart part in movableParts)
        {
            if (part == null || part.target == null)
                continue;

            if (string.IsNullOrWhiteSpace(part.partId))
            {
                Debug.LogWarning(
                    $"{characterName} has a movable part with no Part ID.",
                    this);

                continue;
            }

            if (!usedIds.Add(part.partId))
            {
                Debug.LogWarning(
                    $"{characterName} has duplicate Part ID '{part.partId}'. Every Part ID must be unique.",
                    this);

                continue;
            }

            SavedPartPose saved =
                new SavedPartPose();

            saved.partId =
                part.partId;

            saved.localPosition =
                part.target.localPosition;

            saved.localEulerAngles =
                NormalizeEuler(
                    part.target.localEulerAngles);

            saved.localScale =
                part.target.localScale;

            pose.parts.Add(saved);
        }

#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif

        Debug.Log(
            $"Saved pose '{poseNameToCapture}' for {characterName}. " +
            $"Captured {pose.parts.Count} movable parts.",
            this);
    }

    private Vector3 NormalizeEuler(Vector3 euler)
    {
        euler.x = NormalizeAngle(euler.x);
        euler.y = NormalizeAngle(euler.y);
        euler.z = NormalizeAngle(euler.z);

        return euler;
    }

    private float NormalizeAngle(float angle)
    {
        if (angle > 180f)
            angle -= 360f;

        return angle;
    }

    // =========================================================
    // TEST COMMANDS
    // =========================================================

    [ContextMenu("TEST - Expression")]
    private void TestCurrentExpression()
    {
        SetExpression(testExpression);
    }

    [ContextMenu("TEST - Pose Instant")]
    private void TestCurrentPoseInstant()
    {
        SetPoseInstant(testPose);
    }

    [ContextMenu("TEST - Pose Animated")]
    private void TestCurrentPoseAnimated()
    {
        SetPose(testPose);
    }
}