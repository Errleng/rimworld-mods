using RimWorld;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace HighDensityHydroCustom
{
    internal class Command_SetValue : Command
    {
        public Action<int> onValueChange;
        public int minVal;
        public int maxVal;

        public override void ProcessInput(Event ev)
        {
            base.ProcessInput(ev);

            Dialog_TextInput dialog = new Dialog_TextInput
            {
                setValue = (string val) =>
                {
                    int num = -1;
                    if (int.TryParse(val, out num))
                    {
                        if (num >= minVal && num <= maxVal)
                        {
                            onValueChange(num);
                            return;
                        }
                    }
                    Messages.Message("HDHInvalidIntField".Translate(minVal, maxVal), MessageTypeDefOf.RejectInput);
                }
            };
            Find.WindowStack.Add(dialog);
        }
    }

    internal class Dialog_TextInput : Window
    {
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

        public Action<string> setValue;

        protected string value;

        // Token: 0x04001960 RID: 6496
        private bool focusField;

        // Token: 0x04001961 RID: 6497
        private int startAcceptingInputAtFrame;
    }
}
