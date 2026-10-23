// SPDX-License-Identifier: MIT

using System.Numerics;
using Celeste.Core.Components;
using Celeste.Core.Reflection.Runtime;
using Celeste.Core.Scheduling;
using Celeste.Everest.Modules.Entities.Metadata;
using Celeste.Formats.Map.Entities.Structure;
using Celeste.Game.Execution.Levels;
using Celeste.Scene.Entities;
using Celeste.Simulation.Runtime.Session;

namespace Celeste.Mods.MaxHelpingHand.Entities.Flags
{
    [CustomEntity("MaxHelpingHand/FlagLogicGate")]
    [RuntimeTypeAlias("Celeste.Mod.MaxHelpingHand.Entities.FlagLogicGate")]
    public class FlagLogicGate : SceneEntityState
    {
        private enum Function
        {
            And,
            Or,
            Xor
        }

        private string outputFlag;
        private readonly string[] inputFlags;
        private readonly bool[] negatedFlags;
        private readonly Function function;
        private readonly bool negated;

        public FlagLogicGate(ISourceEngineContext engine, EntityData data, Vector2 offset)
            : base(((ISourceEntitySourceDataEnvironment)engine).EntitySourceDataRegistration)
        {
            outputFlag = data.Attr("outputFlag", "");
            inputFlags = data.Attr("inputFlags", "").Split(',');
            negatedFlags = new bool[inputFlags.Length];
            for (int i = 0; i < inputFlags.Length; i++)
            {
                if (!inputFlags[i].StartsWith("!")) continue;
                inputFlags[i] = inputFlags[i].Substring(1);
                negatedFlags[i] = true;
            }

            function = data.Attr("func", "") switch
            {
                "OR" => Function.Or,
                "XOR" => Function.Xor,
                _ => Function.And
            };
            negated = data.Bool("not", false);
        }

        public override void Update()
        {
            base.Update();
            var session = ((OriginalLevelSceneRoot)Scene).Session;
            bool value = Evaluate(session);
            session.SetFlag(outputFlag, negated ? !value : value);
        }

        private bool Evaluate(ChapterCompletionSession session)
        {
            switch (function)
            {
                case Function.Or:
                    for (int i = 0; i < inputFlags.Length; i++)
                        if (Read(session, i))
                            return true;
                    return false;
                case Function.Xor:
                    int count = 0;
                    for (int i = 0; i < inputFlags.Length; i++)
                        if (Read(session, i))
                            count++;
                    return count == 1;
                default:
                    for (int i = 0; i < inputFlags.Length; i++)
                        if (!Read(session, i))
                            return false;
                    return true;
            }
        }

        private bool Read(ChapterCompletionSession session, int index)
        {
            bool value = session.GetFlag(inputFlags[index]);
            return negatedFlags[index] ? !value : value;
        }
    }
}
