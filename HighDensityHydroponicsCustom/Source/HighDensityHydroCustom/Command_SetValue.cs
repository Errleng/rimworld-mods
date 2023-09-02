using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace HighDensityHydroCustom
{
    internal class Command_SetValue : Command
    {
        public Action<int> onValueChange;
        public int initialVal;
        public int minVal;
        public int maxVal;

        private List<Action<int>> onValueChanges = new List<Action<int>>();

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);

            if (!onValueChanges.Contains(onValueChange))
            {
                onValueChanges.Add(onValueChange);
            }

            Dialog_TextInput dialog = new Dialog_TextInput
            {
                initialVal = initialVal.ToString(),
                setValue = (string val) =>
                {
                    int num = -1;
                    if (int.TryParse(val, out num))
                    {
                        foreach (var action in onValueChanges)
                        {
                            action(Math.Min(Math.Max(num, minVal), maxVal));
                        }
                        if (num >= minVal && num <= maxVal)
                        {
                            return;
                        }
                    }
                    Messages.Message("HDHInvalidIntField".Translate(minVal, maxVal), MessageTypeDefOf.RejectInput);
                }
            };
            Find.WindowStack.Add(dialog);
        }

        public override bool InheritInteractionsFrom(Gizmo other)
        {
            Command_SetValue command = other as Command_SetValue;
            if (command != null && !onValueChanges.Contains(command.onValueChange))
            {
                onValueChanges.Add(command.onValueChange);
            }
            return false;
        }
    }

    internal class Dialog_TextInput : Window
    {
        public Action<string> setValue;
        public string initialVal;

        private string value;
        private bool focusField;
        private int startAcceptingInputAtFrame;

        private bool AcceptsInput
        {
            get
            {
                return startAcceptingInputAtFrame <= Time.frameCount;
            }
        }

        protected virtual int MaxLength
        {
            get
            {
                return 100;
            }
        }

        public override Vector2 InitialSize
        {
            get
            {
                return new Vector2(280f, 175f);
            }
        }

        public Dialog_TextInput()
        {
            forcePause = true;
            doCloseX = true;
            absorbInputAroundWindow = true;
            closeOnAccept = false;
            closeOnClickedOutside = true;
            value = initialVal;
        }

        public void WasOpenedByHotkey()
        {
            startAcceptingInputAtFrame = Time.frameCount + 1;
        }

        protected virtual AcceptanceReport IsValid(string value)
        {
            if (value.Length == 0)
            {
                return false;
            }
            return true;
        }

        public override void DoWindowContents(Rect inRect)
        {
            Text.Font = GameFont.Small;
            bool flag = false;
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
            {
                flag = true;
                Event.current.Use();
            }
            GUI.SetNextControlName("TextField");
            string text = Widgets.TextField(new Rect(0f, 15f, inRect.width, 35f), value);
            if (AcceptsInput && text.Length < MaxLength)
            {
                value = text;
            }
            else if (!AcceptsInput)
            {
                ((TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl)).SelectAll();
            }
            if (!focusField)
            {
                UI.FocusControl("TextField", this);
                focusField = true;
            }
            if (Widgets.ButtonText(new Rect(15f, inRect.height - 35f - 15f, inRect.width - 15f - 15f, 35f), "OK", true, true, true, null) || flag)
            {
                AcceptanceReport acceptanceReport = IsValid(value);
                if (!acceptanceReport.Accepted)
                {
                    if (acceptanceReport.Reason.NullOrEmpty())
                    {
                        Messages.Message("HDHInvalidTextField".Translate(), MessageTypeDefOf.RejectInput, false);
                        return;
                    }
                    Messages.Message(acceptanceReport.Reason, MessageTypeDefOf.RejectInput, false);
                    return;
                }
                else
                {
                    setValue(value);
                    Find.WindowStack.TryRemove(this, true);
                }
            }
        }
    }
}
