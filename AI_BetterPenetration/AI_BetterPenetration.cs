using BepInEx;
using BepInEx.Configuration;
using BepInEx.Harmony;
using BepInEx.Logging;
using HarmonyLib;
using IllusionUtility.SetUtility;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;


namespace AI_BetterPenetration
{

    [BepInPlugin("animal42069.aibetterpenetration", "AI Better Penetration", VERSION)]
    public class AI_BetterPenetration : BaseUnityPlugin
    {
        public const string VERSION = "1.0.0";

        public new static ManualLogSource Logger;

        private static ConfigEntry<float> _dan109Radius;
        private static ConfigEntry<float> _dan109Length;
        private static ConfigEntry<float> _dan101Radius;
        private static ConfigEntry<float> _dan_length;
        private static ConfigEntry<float> _dan_softness;
        private static ConfigEntry<float> _clipping_depth;
        private static ConfigEntry<float> _kokanDamping;
        private static ConfigEntry<float> _kokanElasticity;
        private static ConfigEntry<float> _kokanStiffness;
        private static ConfigEntry<float> _kokanInert;

        public static DynamicBoneCollider dan109DBC;
        public static DynamicBoneCollider dan101DBC;

        public static AIChara.ChaControl[] fem_list;
        public static AIChara.ChaControl[] male_list;
        public static List<DynamicBone> vagBones = new List<DynamicBone>();
        public static Transform dan101;
        public static Transform dan109;
        public static Transform danUp;
        private static bool bDanPenetration;
        private static bool bDansFound;
        private static bool bHPointsFound;
        private static bool inHScene;

        private static Transform referenceLookAtTarget;
        private static Transform replacementLookAtTarget;
        private static Transform hPoint1B, hPoint1F, hPoint1C;
        private static Transform hPoint2B, hPoint2F, hPoint2C;
        private static Transform hPoint3B, hPoint3FL, hPoint3FR, hPoint3C;
        private static Transform hPointBackOfHead;

        private void Awake()
        {
            _dan109Radius = Config.Bind<float>("Penis", "Radius of the penis tip collider", 0.20f, "Set the penis collider's radius, change depending on uncensor");
            _dan109Length = Config.Bind<float>("Penis", "Length of the penis tip collider", 0.5f, "Set the penis collider's height, change depending on uncensor");
            _dan101Radius = Config.Bind<float>("Penis", "Radius of the penis shaft collider", 0.215f, "Set the penis collider's radius, change depending on uncensor");
            _dan_length = Config.Bind<float>("Penis", "Length of Penis", 2.0f, "Set the length of the penis. 2 = 20 cm or 8 inches.");
            _dan_softness = Config.Bind<float>("Penis", "Softness of the penis", 0.0f, "Set the softness of the penis.  A value of 0 means maximum hardness, the penis will remain the same length at all times.  A value greater than 0 will cause the penis to squish/shrink after penetration, the higher the value (maximum of 1), the more squish.");

            _clipping_depth = Config.Bind<float>("Clipping", "Depth inside body to clip", 0.25f, "Set how close to body surface to limit penis for clipping purposes. Smaller values will result in more clipping through the body, larger values will make the shaft wander further away from the intended penetration point.");

            _kokanDamping = Config.Bind<float>("Vagina", "Damping of the vagina dynamic bones", 0.2f, "Set the damping value of the vagina dynamic bones.");
            _kokanElasticity = Config.Bind<float>("Vagina", "Elasticity of the vagina dynamic bones", 0.1f, "Set the elasticity value of the vagina dynamic bones.");
            _kokanStiffness = Config.Bind<float>("Vagina", "Stiffness of the vagina dynamic bones", 0.1f, "Set the stiffness value of the vagina dynamic bones.");
            _kokanInert = Config.Bind<float>("Vagina", "Inert of the vagina dynamic bones", 0.85f, "Set the inert value of the vagina dynamic bones.");

            Logger = base.Logger;

            _dan109Radius.SettingChanged += delegate
            {
                if (inHScene)
                {
                    dan109DBC.m_Radius = _dan109Radius.Value;
                }
            };

            _dan109Length.SettingChanged += delegate
            {
                if (inHScene)
                {
                    dan109DBC.m_Height = _dan109Length.Value;
                    dan101DBC.m_Center = new Vector3(0, 0, (_dan_length.Value - _dan109Length.Value) / 2);
                    dan101DBC.m_Height = _dan_length.Value - _dan109Length.Value;
                }
            };

            _dan101Radius.SettingChanged += delegate
            {
                if (inHScene)
                {
                    dan101DBC.m_Radius = _dan101Radius.Value;
                }
            };

            _dan_length.SettingChanged += delegate
            {
                if (inHScene)
                {
                    dan101DBC.m_Center = new Vector3(0, 0, (_dan_length.Value - _dan109Length.Value) / 2);
                    dan101DBC.m_Height = _dan_length.Value - _dan109Length.Value;
                }
            };

            _kokanDamping.SettingChanged += delegate
            {
                if (inHScene && vagBones != null)
                {
                    foreach (DynamicBone vagBone in vagBones)
                    {
                        if (vagBone != null)
                        {
                            vagBone.m_Damping = _kokanDamping.Value;
                        }
                    }
                }
            };

            _kokanElasticity.SettingChanged += delegate
            {
                if (inHScene && vagBones != null)
                {
                    foreach (DynamicBone vagBone in vagBones)
                    {
                        if (vagBone != null)
                        {
                            vagBone.m_Elasticity = _kokanElasticity.Value;
                        }
                    }
                }
            };

            _kokanStiffness.SettingChanged += delegate
            {
                if (inHScene && vagBones != null)
                {
                    foreach (DynamicBone vagBone in vagBones)
                    {
                        if (vagBone != null)
                        {
                            vagBone.m_Stiffness = _kokanStiffness.Value;
                        }
                    }
                }
            };

            _kokanInert.SettingChanged += delegate
            {
                if (inHScene && vagBones != null)
                {
                    foreach (DynamicBone vagBone in vagBones)
                    {
                        if (vagBone != null)
                        {
                            vagBone.m_Inert = _kokanInert.Value;
                        }
                    }
                }
            };

            var harmony = new Harmony("AI_BetterPenetration");
            HarmonyWrapper.PatchAll(typeof(AI_BetterPenetration), harmony);
        }

        [HarmonyPrefix, HarmonyPatch(typeof(H_Lookat_dan), "setInfo")]
        private static void EarlyReplaceKokan()
        {
            bDanPenetration = false;
        }

        [HarmonyPostfix, HarmonyPatch(typeof(H_Lookat_dan), "setInfo")]
        private static void HScene_ChangeMotion(H_Lookat_dan __instance)
        {
            if (__instance == null || !bDansFound || !bHPointsFound)
                return;

            if (__instance.transLookAtNull != null && __instance.strPlayMotion.Contains("Idle") == false && __instance.strPlayMotion.Contains("OUT") == false)
            {
                bDanPenetration = true;
                referenceLookAtTarget = __instance.transLookAtNull;
            }
            else
            {
                referenceLookAtTarget = dan109;
            }

            if (replacementLookAtTarget == null)
            {
                replacementLookAtTarget = Instantiate(referenceLookAtTarget, referenceLookAtTarget.position, referenceLookAtTarget.rotation);
                replacementLookAtTarget.name = "virtual_target";
                replacementLookAtTarget.parent = null;
            }

            if (replacementLookAtTarget != null)
            {
                replacementLookAtTarget.SetPosition(referenceLookAtTarget.position.x, referenceLookAtTarget.position.y, referenceLookAtTarget.position.z);
            }

            SetDanTarget(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(H_Lookat_dan), "LateUpdate")]
        public static void OffsetPenisTarget(H_Lookat_dan __instance)
        {
            if (__instance == null || !bDansFound || !bHPointsFound)
                return;

            SetDanTarget(__instance);
        }

        [HarmonyPostfix, HarmonyPatch(typeof(HScene), "SetStartVoice")]
        public static void AddPColliders(HScene __instance)
        {
            inHScene = true;
            bDanPenetration = false;
            bDansFound = false;
            bHPointsFound = false;

            male_list = __instance.GetMales().Where(male => male != null).ToArray();
            fem_list = __instance.GetFemales().Where(female => female != null).ToArray();

            foreach (var female in fem_list.Where(female => female != null))
            {
                hPoint1B = female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cf_J_sk_04_01")).FirstOrDefault();
                hPoint1F = female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("k_f_kosi03_03")).FirstOrDefault();
                hPoint1C = female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("k_f_kosi02_03")).FirstOrDefault();
                hPoint2B = female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("N_Waist_b")).FirstOrDefault();
                hPoint2F = female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("N_Waist_f")).FirstOrDefault();
                hPoint2C = female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("k_f_kosi01_03")).FirstOrDefault();
                hPoint3B = female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("N_Back")).FirstOrDefault();
                hPoint3FL = female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cf_J_Mune00_d_R")).FirstOrDefault();
                hPoint3FR = female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cf_J_Mune00_d_R")).FirstOrDefault();
                hPoint3C = female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("k_f_spine02_03")).FirstOrDefault();
                hPointBackOfHead = female.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("k_f_head_03")).FirstOrDefault();
            }

            if (hPoint1F != null && hPoint1C != null && hPoint2B != null && hPoint2C != null && hPoint3B != null && hPoint3FL != null && hPoint3FR != null && hPoint3C != null && hPointBackOfHead != null)
                bHPointsFound = true;

            foreach (var male in male_list.Where(male => male != null))
            {
                danUp = male.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("k_f_kosi03_03")).FirstOrDefault();
                dan101 = male.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cm_J_dan101_00")).FirstOrDefault();
                dan109 = male.GetComponentsInChildren<Transform>().Where(x => x.name.Contains("cm_J_dan109_00")).FirstOrDefault();

                if (dan101 != null && danUp != null && dan109 != null)
                    bDansFound = true;

                dan101DBC = dan101.GetComponent<DynamicBoneCollider>();

                if (dan101DBC == null)
                    dan101DBC = dan101.gameObject.AddComponent(typeof(DynamicBoneCollider)) as DynamicBoneCollider;

                dan101DBC.m_Direction = DynamicBoneColliderBase.Direction.Z;
                dan101DBC.m_Center = new Vector3(0, 0, (_dan_length.Value - _dan109Length.Value) / 2);
                dan101DBC.m_Bound = DynamicBoneColliderBase.Bound.Outside;
                dan101DBC.m_Radius = (float)_dan101Radius.Value;
                dan101DBC.m_Height = _dan_length.Value - _dan109Length.Value;

                dan109DBC = dan109.GetComponent<DynamicBoneCollider>();

                if (dan109DBC == null)
                {
                    dan109DBC = dan109.gameObject.AddComponent(typeof(DynamicBoneCollider)) as DynamicBoneCollider;
                }

                dan109DBC.m_Direction = DynamicBoneColliderBase.Direction.Z;
                dan109DBC.m_Center = new Vector3(0, 0, 0);
                dan109DBC.m_Bound = DynamicBoneColliderBase.Bound.Outside;
                dan109DBC.m_Radius = _dan109Radius.Value;
                dan109DBC.m_Height = _dan109Length.Value;
            }

            foreach (var female in fem_list.Where(female => female != null))
            {
                foreach (DynamicBone db in female.GetComponentsInChildren<DynamicBone>().Where(x => x.name.Contains("cf_J_Vagina")))
                {
                    if (db != null)
                    {
                        db.m_Damping = _kokanDamping.Value;
                        db.m_Elasticity = _kokanElasticity.Value;
                        db.m_Stiffness = _kokanStiffness.Value;
                        db.m_Inert = _kokanInert.Value;

                        vagBones.Add(db);

                        if (db.m_Colliders.Contains(dan101DBC))
                        {
                            Console.WriteLine("Instance of " + dan101DBC.name + " already exists in list for DB " + db.name);
                        }
                        else
                        {
                            db.m_Colliders.Add(dan101DBC);
                            Console.WriteLine(dan101DBC.name + " added to " + female.name + " for bone " + db.name);
                        }
                        if (db.m_Colliders.Contains(dan109DBC))
                        {
                            Console.WriteLine("Instance of " + dan109DBC.name + " already exists in list for DB " + db.name);
                        }
                        else
                        {
                            db.m_Colliders.Add(dan109DBC);
                            Console.WriteLine(dan109DBC.name + " added to " + female.name + " for bone " + db.name);
                        }
                    }
                }
            }
            Console.WriteLine("AddPColliders done.");
        }

        [HarmonyPrefix, HarmonyPatch(typeof(HScene), "EndProc")]
        public static void HScene_EndProc_Patch()
        {
            inHScene = false;
            if (!inHScene)
            {
                if (vagBones.Any())
                {
                    foreach (DynamicBone vagBone in vagBones)
                    {
                        if (vagBone != null)
                        {
                            Console.WriteLine("Clearing colliders from " + vagBone.m_Root.name);
                            vagBone.m_Colliders.Clear();
                        }
                    }
                    Console.WriteLine("Destroying collider " + dan109DBC.name);
                    Destroy(dan109DBC);
                    Console.WriteLine("Destroying collider " + dan101DBC.name);
                    Destroy(dan101DBC);
                    Console.WriteLine("Clearing females list");
                    Array.Clear(fem_list, 0, fem_list.Length);
                    Console.WriteLine("Clearing males list");
                    Array.Clear(male_list, 0, male_list.Length);
                }
            }
        }

        private static void SetDanTarget(H_Lookat_dan __instance)
        {
            if (__instance == null || !bDansFound || !bHPointsFound)
                return;

            if (referenceLookAtTarget == null)
                referenceLookAtTarget = dan109;

            Vector3 dan101_pos = new Vector3(dan101.position.x, dan101.position.y, dan101.position.z);
            Vector3 lookTarget = new Vector3(referenceLookAtTarget.position.x, referenceLookAtTarget.position.y, referenceLookAtTarget.position.z);

            float pdist = Vector3.Distance(dan101_pos, lookTarget);
            float tdist = _dan_length.Value / pdist;

            Vector3 dan109_pos = Vector3.LerpUnclamped(dan101_pos, lookTarget, tdist);

            if (!bDanPenetration)
            {
                dan109.SetPosition(dan109_pos.x, dan109_pos.y, dan109_pos.z);
                dan101.rotation = Quaternion.LookRotation(dan109.position - dan101.position, Vector3.Normalize(danUp.position - dan101.position));
            }
            else
            {
                if (referenceLookAtTarget.name == "k_f_kokan_00" || referenceLookAtTarget.name == "k_f_ana_00")
                {
                    Vector3 hFpos, hBpos;
                    Vector3 h1Bpos;
                    Vector3 h2Bpos;
                    Vector3 h3Bpos;
                    Vector3 h1Fpos;
                    Vector3 h2Fpos;
                    Vector3 h3Fpos;

                    Vector3 hPoint1Fpos = Vector3.Lerp(hPoint1F.position, hPoint1B.position, _clipping_depth.Value / Vector3.Distance(hPoint1F.position, hPoint1B.position));
                    Vector3 hPoint1Bpos = Vector3.Lerp(hPoint1B.position, hPoint1F.position, _clipping_depth.Value / Vector3.Distance(hPoint1F.position, hPoint1B.position));
                    Vector3 hPoint2Fpos = Vector3.Lerp(hPoint2F.position, hPoint2B.position, _clipping_depth.Value / Vector3.Distance(hPoint2F.position, hPoint2B.position));
                    Vector3 hPoint2Bpos = Vector3.Lerp(hPoint2B.position, hPoint2F.position, _clipping_depth.Value / Vector3.Distance(hPoint2F.position, hPoint2B.position));

                    Vector3 hPoint3FLRpos = Vector3.Lerp(hPoint3FL.position, hPoint3FR.position, (float)0.5);

                    Vector3 hPoint3Fpos = Vector3.Lerp(hPoint3FLRpos, hPoint3B.position, _clipping_depth.Value / Vector3.Distance(hPoint3FLRpos, hPoint3B.position));
                    Vector3 hPoint3Bpos = Vector3.Lerp(hPoint3B.position, hPoint3FLRpos, _clipping_depth.Value / Vector3.Distance(hPoint3FLRpos, hPoint3B.position));

                    //float danToH0Fdist = Vector3.Distance(dan101_pos, hPoint1Fpos);
                    float danToH1Fdist = Vector3.Distance(dan101_pos, hPoint1Fpos);
                    float danToH2Fdist = Vector3.Distance(dan101_pos, hPoint2Fpos);
                    float danToH3Fdist = Vector3.Distance(dan101_pos, hPoint3Fpos);
                    float danToH1Bdist = Vector3.Distance(dan101_pos, hPoint1Bpos);
                    float danToH2Bdist = Vector3.Distance(dan101_pos, hPoint2Bpos);
                    float danToH3Bdist = Vector3.Distance(dan101_pos, hPoint3Bpos);

                    float danLength = _dan_length.Value;
                    if (_dan_length.Value > pdist)
                        danLength = _dan_length.Value - (_dan_length.Value - pdist) * _dan_softness.Value;
                    tdist = danLength / pdist;

                    dan109_pos = Vector3.LerpUnclamped(dan101_pos, lookTarget, tdist);

                    if (danLength >= danToH3Fdist)
                    {
                        h1Fpos = Vector3.LerpUnclamped(dan101_pos, hPoint1Fpos, danLength / danToH1Fdist);
                        h2Fpos = Vector3.LerpUnclamped(dan101_pos, hPoint2Fpos, danLength / danToH2Fdist);
                        h3Fpos = Vector3.LerpUnclamped(dan101_pos, hPoint3Fpos, danLength / danToH3Fdist);
                    }
                    else if (danLength > danToH2Fdist)
                    {
                        h1Fpos = Vector3.LerpUnclamped(dan101_pos, hPoint1Fpos, danLength / danToH1Fdist);
                        h2Fpos = Vector3.LerpUnclamped(dan101_pos, hPoint2Fpos, danLength / danToH2Fdist);
                        h3Fpos = Quadratic.SolveQuadratic(hPoint2Fpos, hPoint3Fpos, h2Fpos, danToH2Fdist, danLength);
                    }
                    else if (danLength > danToH1Fdist)
                    {
                        h1Fpos = Vector3.LerpUnclamped(dan101_pos, hPoint1F.position, danLength / danToH1Fdist);
                        h3Fpos = h2Fpos = Quadratic.SolveQuadratic(hPoint1Fpos, hPoint2Fpos, h1Fpos, danToH1Fdist, danLength);
                    }
                    else if (danLength > pdist)
                    {
                        h3Fpos = h2Fpos = h1Fpos = Quadratic.SolveQuadratic(lookTarget, hPoint1Fpos, dan109_pos, pdist, danLength);
                    }
                    else
                    {
                        h3Fpos = h2Fpos = h1Fpos = dan109_pos;
                    }

                    if (danLength >= danToH3Bdist)
                    {
                        h1Bpos = Vector3.LerpUnclamped(dan101_pos, hPoint1Bpos, danLength / danToH1Bdist);
                        h2Bpos = Vector3.LerpUnclamped(dan101_pos, hPoint2Bpos, danLength / danToH2Bdist);
                        h3Bpos = Vector3.LerpUnclamped(dan101_pos, hPoint3Bpos, danLength / danToH3Bdist);
                    }
                    else if (danLength > danToH2Bdist)
                    {
                        h1Bpos = Vector3.LerpUnclamped(dan101_pos, hPoint1Bpos, danLength / danToH1Bdist);
                        h2Bpos = Vector3.LerpUnclamped(dan101_pos, hPoint2Bpos, danLength / danToH2Bdist);
                        h3Bpos = Quadratic.SolveQuadratic(hPoint2Bpos, hPoint3Bpos, h2Bpos, danToH2Bdist, danLength);
                    }
                    else if (danLength > danToH1Bdist)
                    {
                        h1Bpos = Vector3.LerpUnclamped(dan101_pos, hPoint1Bpos, danLength / danToH1Bdist);
                        h3Bpos = h2Bpos = Quadratic.SolveQuadratic(hPoint1Bpos, hPoint2Bpos, h1Bpos, danToH1Bdist, danLength);
                    }
                    else if (danLength > pdist)
                    {
                        h3Bpos = h2Bpos = h1Bpos = Quadratic.SolveQuadratic(lookTarget, hPoint1Bpos, dan109_pos, pdist, danLength);
                    }
                    else
                    {
                        h3Bpos = h2Bpos = h1Bpos = dan109_pos;
                    }

                    hFpos = h3Fpos;
                    if (Vector3.Distance(h2Fpos, h1Bpos) < Vector3.Distance(hFpos, h1Bpos))
                        hFpos = h2Fpos;
                    if (Vector3.Distance(h1Fpos, h1Bpos) < Vector3.Distance(hFpos, h1Bpos))
                        hFpos = h1Fpos;

                    hBpos = h3Bpos;
                    if (Vector3.Distance(h2Bpos, hFpos) < Vector3.Distance(hBpos, hFpos))
                        hBpos = h2Bpos;
                    if (Vector3.Distance(h1Bpos, hFpos) < Vector3.Distance(hBpos, hFpos))
                        hBpos = h1Bpos;

                    float WaistDif = Vector3.Distance(hFpos, hBpos);
                    float DanTopDif = Vector3.Distance(dan109_pos, hFpos);
                    float DanBotDif = Vector3.Distance(dan109_pos, hBpos);

                    if (DanTopDif > WaistDif || DanBotDif > WaistDif)
                    {
                        if (DanTopDif > DanBotDif)
                            dan109_pos = hBpos;
                        else
                            dan109_pos = hFpos;
                    }
                }
                else if (referenceLookAtTarget.name == "k_f_head_00")
                {
                    float danLength = _dan_length.Value;
                    if (_dan_length.Value > pdist)
                        danLength = _dan_length.Value - (_dan_length.Value - pdist) * _dan_softness.Value;
                    tdist = danLength / pdist;

                    dan109_pos = Vector3.LerpUnclamped(dan101_pos, lookTarget, tdist);

                    float max_dist = Vector3.Distance(dan101_pos, hPointBackOfHead.position) - _clipping_depth.Value;
                    float max_tdist = danLength / max_dist;

                    if (pdist > max_dist)
                        dan109_pos = Vector3.LerpUnclamped(dan101_pos, lookTarget, max_tdist);
                }

                replacementLookAtTarget.SetPosition(dan109_pos.x, dan109_pos.y, dan109_pos.z);

                __instance.transLookAtNull = replacementLookAtTarget;
                __instance.dan_Info.SetTargetTransform(replacementLookAtTarget);

                dan101.rotation = Quaternion.LookRotation(replacementLookAtTarget.position - dan101.position, Vector3.Normalize(danUp.position - dan101.position));
            }
        }
    }
}