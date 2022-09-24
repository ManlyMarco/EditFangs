﻿using System;
using System.Collections.Generic;
using UnityEngine;
using BepInEx.Logging;
using KKAPI;
using KKAPI.Chara;
using ExtensibleSaveFormat;
using MessagePack;
using System.Collections;

namespace EditFangs
{
    public class EditFangsController : CharaCustomFunctionController
    {
        internal static ManualLogSource Logger = EditFangsPlugin.Logger;

        internal Vector3[] fangsBaseVertices = new Vector3[42];
        internal Vector3[] fangsBaseNormals = new Vector3[42];
        private bool registered;
        private bool loaded;
        public FangData fangData = new FangData { dirty = false };
        private int[] tips = new int[2];
        private int[] rotp = new int[2];
        private int headID = 1;

        private GameObject fangsObject;
        private GameObject GetFangsObject()
        {
            //return fangsObject != null ? fangsObject : fangsObject = ChaControl.objHead?.transform.Find("N_tonn_face/N_cf_haed")?.FindLoop("cf_O_canine")?.gameObject;
            return fangsObject != null ? fangsObject : fangsObject = ChaControl.objHead?.transform.Find("N_tonn_face/N_cf_haed/cf_O_canine")?.gameObject;
        }

        protected override void Update()
        {
            base.Update();
            if (registered && fangData.dirty)
            {
                fangData.dirty = false;
                adjustFang(fangData);
            }
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            if (fangData.IsEmpty())
            {
                SetExtendedData(null);
            }
            else
            {
                var data = new PluginData();

                data.data.Add("fangData", MessagePackSerializer.Serialize(fangData));

                //Logger.LogDebug($"Saving extended data for {ChaControl.chaFile.parameter.fullname}: ({fangData.scaleL} | {fangData.spacingL} | {fangData.scaleR} | {fangData.spacingR})");

                SetExtendedData(data);
            }
        }

        protected override void OnReload(GameMode currentGameMode, bool maintainState)
        {
            if (maintainState)
                return;

            if (!loaded && !registered)
            {
                if (!registerFangs())
                    return;
            }

            fangData = new FangData { dirty = fangData.IsEmpty() };

            var data = GetExtendedData();
            if (data == null)
                return;

            //Logger.LogDebug($"Loading extended data for {ChaControl.chaFile.parameter.fullname}");

            // clone the loaded mesh to make sure that it's mesh is not shared with other characters
            var fangs = GetFangsObject();
            fangs.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh = Instantiate(fangs.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh);

            if (data.data.TryGetValue("fangData", out var fangDataSerialised) && fangDataSerialised != null)
                fangData = MessagePackSerializer.Deserialize<FangData>((byte[])fangDataSerialised);

            loaded = true;
            //StartCoroutine(adjustFangDelayed(0.5f, true));
        }
        internal bool registerFangs()
        {
            var fangs = GetFangsObject();
            var mesh = fangs?.GetComponentInChildren<SkinnedMeshRenderer>()?.sharedMesh;
            if (mesh == null) return false;
            float lowestY = 100;
            var tipI = new List<int>();
            if (ChaControl.fileFace.headId != headID)
            {
                headID = ChaControl.fileFace.headId;
                for (var i = 0; i < mesh.vertices.Length; i++)
                {
                    fangsBaseVertices[i] = new Vector3(mesh.vertices[i].x, mesh.vertices[i].y, mesh.vertices[i].z);
                    fangsBaseNormals[i] = new Vector3(mesh.normals[i].x, mesh.normals[i].y, mesh.normals[i].z);
                    if (mesh.vertices[i].y < lowestY) lowestY = mesh.vertices[i].y;
                }
                for (var i = 0; i < mesh.vertices.Length; i++)
                {
                    if (mesh.vertices[i].y == lowestY) tipI.Add(i);
                    if (mesh.vertices[i] == new Vector3(-0.007559223f, 0.01637148f, 0.08396365f)) rotp[0] = i;
                    if (mesh.vertices[i] == new Vector3(0.007559223f, 0.01637148f, 0.08396365f)) rotp[1] = i;
                }
                if (tips.Length == 2)
                {
                    tips[0] = tipI[0];
                    tips[1] = tipI[1];
                }
                else return false;
            }
            registered = true;
            return true;
        }

        /// <summary>
        /// Adjusts the fang's mesh according to the parameters
        /// </summary>
        /// <param name="scaleL">Length of the left fang [0 ; 1]</param>
        /// <param name="spacingL">Spacing of the left fang [0 ; 1.3]</param>
        /// <param name="scaleR">Length of the right fang [0 ; 1]</param>
        /// <param name="spacingR">Spacing of the right fang [0 ; 1.3]</param>
        /// <param name="readjust">Whenever the fang should be adjusted twice (fixes glitchy rotation)</param>
        public void adjustFang(float scaleL, float spacingL, float scaleR, float spacingR, bool readjust = false)
        {
            if (!registered) return;
            var fangs = GetFangsObject();
            if (fangs == null) return;
            var mesh = fangs.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh;
            if (mesh.vertexCount != 42) return;

            if (KoikatuAPI.GetCurrentGameMode() != GameMode.Maker) //dont spam the log to hard
            {
                Logger.LogDebug($"adjusting {ChaControl.chaFile.parameter.fullname}: ({scaleL} | {spacingL} | {scaleR} | {spacingR})");
            }

            var vertices = new Vector3[42];
            var normals = new Vector3[42];

            // special factors used to move and rotate the fang according to the spacing
            var spacingModL = (float)Math.Cos(spacingL * Math.PI / 2) * (float)((spacingL <= 1f) ? 1 : 2.3);
            var spacingModR = (float)Math.Cos(spacingR * Math.PI / 2) * (float)((spacingR <= 1f) ? 1 : 2.3);

            // point around which the left fang rotates
            var rotationPointL = mesh.vertices[rotp[0]] * 0.99f;
            // left rotation
            var rotateL = Quaternion.Euler(0, spacingModL * 30, 0);

            // point around which the rigth fang rotates
            var rotationPointR = mesh.vertices[rotp[1]] * 0.99f;
            // right rotation
            var rotateR = Quaternion.Euler(0, spacingModR * -30, 0);

            // calculation
            for (var i = 0; i < mesh.vertices.Length; i++)
            {
                normals[i] = new Vector3(fangsBaseNormals[i].x, fangsBaseNormals[i].y, fangsBaseNormals[i].z);
                if (i >= 21) // right fang
                {
                    // set vector
                    vertices[i] = new Vector3(fangsBaseVertices[i].x, fangsBaseVertices[i].y, fangsBaseVertices[i].z);
                    // position
                    vertices[i].x -= 0.01f * (1 - spacingR);
                    vertices[i].y -= 0.001f * spacingModR;
                    vertices[i].z += 0.0014f * spacingModR;
                    // rotation
                    vertices[i] = rotationPointR + rotateR * (vertices[i] - rotationPointR);
                    normals[i] = rotateR * normals[i];

                    if (i == tips[1]) // fang tip length
                    {
                        vertices[i].y += 0.01f * (0.1f - scaleR);
                        vertices[i].z += 0.002f * (0.1f - scaleR);
                    }
                }
                else // left fang
                {
                    // set vector
                    vertices[i] = new Vector3(fangsBaseVertices[i].x, fangsBaseVertices[i].y, fangsBaseVertices[i].z);
                    // postition
                    vertices[i].x += 0.01f * (1 - spacingL);
                    vertices[i].y -= 0.001f * spacingModL;
                    vertices[i].z += 0.0014f * spacingModL;
                    // rotation
                    vertices[i] = rotationPointL + rotateL * (vertices[i] - rotationPointL);
                    normals[i] = rotateL * normals[i];

                    if (i == tips[0]) // fang tip length
                    {
                        vertices[i].y += 0.01f * (0.1f - scaleL);
                        vertices[i].z += 0.002f * (0.1f - scaleL);
                    }
                }
            }

            mesh.vertices = vertices;
            mesh.normals = normals;
            fangs.GetComponentInChildren<SkinnedMeshRenderer>().sharedMesh = mesh;

            // readjust (because my math sucks)
            if (readjust)
                adjustFang(fangData);
        }
        /// <summary>
        /// Adjusts the fang's mesh according to the passed FangData
        /// </summary>
        /// <param name="newFangData">FangData containing the 4 parameteres</param>
        /// <param name="readjust">Whenever the fang should be adjusted twice (fixes glitchy rotation)</param>
        public void adjustFang(FangData newFangData, bool readjust = false)
        {
            adjustFang(newFangData.scaleL, newFangData.spacingL, newFangData.scaleR, newFangData.spacingR, readjust);
        }
    }
}
