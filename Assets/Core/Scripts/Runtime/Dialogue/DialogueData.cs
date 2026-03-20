using UnityEngine;
using System;
using System.Collections.Generic;

namespace Blocks.Gameplay.Core
{
    /// <summary>
    /// Represents a single line of dialogue in a conversation.
    /// </summary>
    [Serializable]
    public class DialogueLine
    {
        [Tooltip("Name of the speaker (NPC name or player name).")]
        public string speakerName;

        [Tooltip("The dialogue text content.")]
        [TextArea(2, 5)]
        public string text;

        [Tooltip("If true, this line is spoken by the player (Thánh Gióng).")]
        public bool isPlayerLine;

        [Tooltip("Âm thanh lồng tiếng cho câu này.")]
        public AudioClip voiceClip;

        [Tooltip("Optional delay before showing next line (seconds).")]
        public float delayBeforeNext = 0.5f;
    }

    /// <summary>
    /// A complete dialogue conversation consisting of multiple lines.
    /// ScriptableObject so it can be shared and edited in the Inspector.
    /// </summary>
    [CreateAssetMenu(fileName = "NewDialogue", menuName = "Thanh Giong/Dialogue Data")]
    public class DialogueData : ScriptableObject
    {
        [Tooltip("Display name of the NPC this dialogue belongs to.")]
        public string npcName;

        [Tooltip("List of dialogue lines in order.")]
        public List<DialogueLine> lines = new List<DialogueLine>();

        [Tooltip("If true, this dialogue can only be triggered once.")]
        public bool playOnce = true;
    }
}
