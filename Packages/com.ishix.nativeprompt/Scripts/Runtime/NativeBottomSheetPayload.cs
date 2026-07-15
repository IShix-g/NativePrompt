using System;
using UnityEngine;

namespace NativePrompt
{
    [Serializable]
    internal sealed class NativeBottomSheetPayload
    {
        public string title;
        public string content;
        public string cancelButtonText;
        public NativeBottomSheetActionPayload[] actions;

        internal static string ToJson(BottomSheetOptions options)
        {
            var payload = new NativeBottomSheetPayload
            {
                title = options.Title,
                content = options.Content,
                cancelButtonText = options.CancelButtonText,
                actions = new NativeBottomSheetActionPayload[options.Actions.Length]
            };

            for (int index = 0; index < options.Actions.Length; index++)
            {
                BottomSheetAction action = options.Actions[index];
                payload.actions[index] = new NativeBottomSheetActionPayload
                {
                    id = action.Id,
                    text = action.Text,
                    style = (int)action.Style,
                    enabled = action.Enabled
                };
            }

            return JsonUtility.ToJson(payload);
        }
    }

    [Serializable]
    internal sealed class NativeBottomSheetActionPayload
    {
        public string id;
        public string text;
        public int style;
        public bool enabled;
    }
}
