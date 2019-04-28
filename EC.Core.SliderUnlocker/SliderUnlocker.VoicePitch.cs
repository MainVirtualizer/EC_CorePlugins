﻿using System.Collections.Generic;
using System.Reflection.Emit;
using ChaCustom;
using Harmony;

namespace EC.Core.SliderUnlocker
{
    internal static class VoicePitch
    {
        private const float VanillaPitchLower = 0.94f;
        private const float VanillaPitchUpper = 1.06f;
        private const float VanillaPitchRange = VanillaPitchUpper - VanillaPitchLower;

        private const int ExtendedRangeLower = -500;
        private const int ExtendedRangeUpper = 500;

        public static void Init()
        {
            BepInEx.Harmony.HarmonyWrapper.PatchAll(typeof(VoicePitch));

            var iteratorType = typeof(CvsChara).GetNestedType("<SetInputText>c__Iterator0", AccessTools.all);
            var iteratorMethod = AccessTools.Method(iteratorType, "MoveNext");
            var transpiler = new HarmonyMethod(typeof(VoicePitch), nameof(voicePitchTpl));
            BepInEx.Harmony.HarmonyWrapper.DefaultInstance.Patch(iteratorMethod, null, null, transpiler);
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(ChaFileParameter), "get_voicePitch")]
        public static bool voicePitchHook(ChaFileParameter __instance, ref float __result)
        {
            // Replace line return Mathf.Lerp(0.94f, 1.06f, this.voiceRate);
            __result = VanillaPitchLower + __instance.voiceRate * VanillaPitchRange;
            return false;
        }

        [HarmonyTranspiler]
        [HarmonyPatch(typeof(CvsChara), "<Start>m__B")]
        public static IEnumerable<CodeInstruction> mB(IEnumerable<CodeInstruction> _instructions)
        {
            // Changes constants in line this.inpPitchPow.text = CustomBase.ConvertTextFromRate(0, 100, this.sldPitchPow.value);
            var instructions = new List<CodeInstruction>(_instructions).ToArray();
            instructions[15].operand = (float) ExtendedRangeLower;
            instructions[16].operand = (float) ExtendedRangeUpper;
            return instructions;
        }

        public static IEnumerable<CodeInstruction> voicePitchTpl(IEnumerable<CodeInstruction> _instructions)
        {
            // Changes constants in line this.inpPitchPow.text = CustomBase.ConvertTextFromRate(0, 100, this.param.voiceRate);
            var instructions = new List<CodeInstruction>(_instructions).ToArray();
            instructions[25].opcode = OpCodes.Ldc_I4;
            instructions[25].operand = ExtendedRangeLower;
            instructions[26].opcode = OpCodes.Ldc_I4;
            instructions[26].operand = ExtendedRangeUpper;
            return instructions;
        }
    }
}
