/* 
 * Copyright (C) 2021 Victor Soupday
 * This file is part of CC3_Unity_Tools <https://github.com/soupday/cc3_unity_tools>
 * 
 * CC3_Unity_Tools is free software: you can redistribute it and/or modify
 * it under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * CC3_Unity_Tools is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with CC3_Unity_Tools.  If not, see <https://www.gnu.org/licenses/>.
 */

using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using System.IO;
using System.Collections.Generic;

namespace Reallusion.Import
{
    public static class MeshUtil
    {
        public const string INVERTED_FOLDER_NAME = "Inverted Meshes";
        public const string PRUNED_FOLDER_NAME = "Pruned Meshes";
        public const string MESH_FOLDER_NAME = "Meshes";

        [MenuItem("CC3/Tools/Reverse Triangle Order", priority = 100)]
        private static void DoReverse()
        {
            MeshUtil.ReverseTriangleOrder(Selection.activeObject);
        }

        [MenuItem("CC3/Tools/Prune Blend Shapes", priority = 101)]
        private static void DoPrune()
        {
            MeshUtil.PruneBlendShapes(Selection.activeObject);
        }

        [MenuItem("CC3/Tools/Open or Close Character Mouth", priority = 201)]
        private static void DoOpenCloseMouth()
        {
            MeshUtil.CharacterOpenCloseMouth(Selection.activeObject);
        }

        [MenuItem("CC3/Tools/Open or Close Character Eyes", priority = 202)]
        private static void DoOpenCloseEyes()
        {
            MeshUtil.CharacterOpenCloseEyes(Selection.activeObject);
        }

        [MenuItem("CC3/Tools/Eye/Look Left", priority = 203)]
        private static void DoLookLeft()
        {
            MeshUtil.CharacterEyeLook(Selection.activeObject, EyeLookDir.Left);
        }

        [MenuItem("CC3/Tools/Eye/Look Right", priority = 204)]
        private static void DoLookRight()
        {
            MeshUtil.CharacterEyeLook(Selection.activeObject, EyeLookDir.Right);
        }

        [MenuItem("CC3/Tools/Eye/Look Up", priority = 205)]
        private static void DoLookUp()
        {
            MeshUtil.CharacterEyeLook(Selection.activeObject, EyeLookDir.Up);
        }

        [MenuItem("CC3/Tools/Eye/Look Down", priority = 206)]
        private static void DoLookDown()
        {
            MeshUtil.CharacterEyeLook(Selection.activeObject, EyeLookDir.Down);
        }

        [MenuItem("CC3/Tools/Eye/Look Forward", priority = 207)]
        private static void DoLookForward()
        {
            MeshUtil.CharacterEyeLook(Selection.activeObject, EyeLookDir.None);
        }

#if HDRP_10_5_0_OR_NEWER
        [MenuItem("CC3/Tools/Add HDRP Diffusion Profiles", priority = 180)]
        private static void DoAddDiffusionProfiles()
        {
            Pipeline.AddDiffusionProfilesHDRP();
        }
#endif

        public static Mesh GetMeshFrom(Object obj)
        {
            if (obj.GetType() == typeof(Mesh))
            {
                Mesh m = (Mesh)obj;
                if (m) return m;
            }

            if (obj.GetType() == typeof(GameObject))
            {
                GameObject go = (GameObject)obj;
                if (go)
                {
                    Mesh m = go.GetComponent<Mesh>();
                    if (m) return m;
                    
                    MeshFilter mf = go.GetComponent<MeshFilter>();
                    if (mf)
                    {
                        return mf.mesh;
                    }
                    
                    SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
                    if (smr)
                    {
                        return smr.sharedMesh;
                    }
                }
            }

            return null;
        }

        public static bool ReplaceMesh(Object obj, Mesh mesh)
        {
            if (obj.GetType() == typeof(GameObject))
            {
                GameObject go = (GameObject)obj;
                if (go)
                {
                    MeshFilter mf = go.GetComponent<MeshFilter>();
                    if (mf)
                    {
                        mf.mesh = mesh;
                        return true;
                    }

                    SkinnedMeshRenderer smr = go.GetComponent<SkinnedMeshRenderer>();
                    if (smr)
                    {
                        smr.sharedMesh = mesh;
                        return true;
                    }
                }
            }

            return false;
        }

        public static void PruneBlendShapes(Object obj)
        {
            if (!obj) return;

            GameObject sceneRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
            GameObject asset = PrefabUtility.GetCorrespondingObjectFromSource(sceneRoot);
            Object srcObj = PrefabUtility.GetCorrespondingObjectFromSource(obj);
            Mesh srcMesh = GetMeshFrom(srcObj);
            string path = AssetDatabase.GetAssetPath(asset);

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("Object: " + obj.name + " has no source Prefab Asset.");
                path = Path.Combine("Assets", "dummy.prefab");
            }

            if (!srcMesh)
            {
                Debug.LogError("No mesh found in selected object.");
                return;
            }

            string folder = Path.GetDirectoryName(path);
            string meshFolder = Path.Combine(folder, PRUNED_FOLDER_NAME);

            Mesh dstMesh = new Mesh();
            dstMesh.vertices = srcMesh.vertices;
            dstMesh.uv = srcMesh.uv;
            dstMesh.uv2 = srcMesh.uv2;
            dstMesh.normals = srcMesh.normals;
            dstMesh.colors = srcMesh.colors;
            dstMesh.boneWeights = srcMesh.boneWeights;
            dstMesh.bindposes = srcMesh.bindposes;
            dstMesh.bounds = srcMesh.bounds;
            dstMesh.tangents = srcMesh.tangents;
            dstMesh.triangles = srcMesh.triangles;
            dstMesh.subMeshCount = srcMesh.subMeshCount;

            for (int s = 0; s < srcMesh.subMeshCount; s++)
            {
                SubMeshDescriptor submesh = srcMesh.GetSubMesh(s);
                dstMesh.SetSubMesh(s, submesh);
            }

            // copy any blendshapes across
            if (srcMesh.blendShapeCount > 0)
            {
                Vector3[] deltaVerts = new Vector3[srcMesh.vertexCount];
                Vector3[] deltaNormals = new Vector3[srcMesh.vertexCount];
                Vector3[] deltaTangents = new Vector3[srcMesh.vertexCount];

                for (int i = 0; i < srcMesh.blendShapeCount; i++)
                {
                    string name = srcMesh.GetBlendShapeName(i);

                    int frameCount = srcMesh.GetBlendShapeFrameCount(i);
                    for (int f = 0; f < frameCount; f++)
                    {
                        float frameWeight = srcMesh.GetBlendShapeFrameWeight(i, f);
                        srcMesh.GetBlendShapeFrameVertices(i, f, deltaVerts, deltaNormals, deltaTangents);

                        Vector3 deltaSum = Vector3.zero;
                        for (int d = 0; d < srcMesh.vertexCount; d++) deltaSum += deltaVerts[d];
                        //Debug.Log(name + ": deltaSum = " + deltaSum.ToString());
                        
                        if (deltaSum.magnitude > 0.1f)
                            dstMesh.AddBlendShapeFrame(name, frameWeight, deltaVerts, deltaNormals, deltaTangents);
                    }
                }
            }

            // Save the mesh asset.
            if (!AssetDatabase.IsValidFolder(meshFolder))
                AssetDatabase.CreateFolder(folder, PRUNED_FOLDER_NAME);
            string meshPath = Path.Combine(meshFolder, srcObj.name + ".mesh");
            AssetDatabase.CreateAsset(dstMesh, meshPath);

            if (obj.GetType() == typeof(GameObject))
            {
                GameObject go = (GameObject)obj;
                if (go)
                {
                    Mesh createdMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

                    if (!ReplaceMesh(obj, createdMesh))
                    {
                        Debug.LogError("Unable to set mesh in selected object!");
                    }
                }
            }
        }

        public static void ReverseTriangleOrder(Object obj)
        {
            if (!obj) return;

            GameObject sceneRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
            GameObject asset = PrefabUtility.GetCorrespondingObjectFromSource(sceneRoot);
            Object srcObj = PrefabUtility.GetCorrespondingObjectFromSource(obj);
            Mesh srcMesh = GetMeshFrom(srcObj);
            string path = AssetDatabase.GetAssetPath(asset);

            if (string.IsNullOrEmpty(path))
            {
                Debug.LogWarning("Object: " + obj.name + " has no source Prefab Asset.");
                path = Path.Combine("Assets", "dummy.prefab");
            }

            if (!srcMesh)
            {
                Debug.LogError("No mesh found in selected object.");
                return;
            }

            string folder = Path.GetDirectoryName(path);
            string meshFolder = Path.Combine(folder, INVERTED_FOLDER_NAME);

            Mesh dstMesh = new Mesh();
            dstMesh.vertices = srcMesh.vertices;
            dstMesh.uv = srcMesh.uv;
            dstMesh.uv2 = srcMesh.uv2;
            dstMesh.normals = srcMesh.normals;
            dstMesh.colors = srcMesh.colors;
            dstMesh.boneWeights = srcMesh.boneWeights;
            dstMesh.bindposes = srcMesh.bindposes;
            dstMesh.bounds = srcMesh.bounds;
            dstMesh.tangents = srcMesh.tangents;            

            int[] reversed = new int[srcMesh.triangles.Length];
            int[] forward = srcMesh.triangles;

            // first pass: reverse the triangle order for each submesh
            for (int s = 0; s < srcMesh.subMeshCount; s++)
            {
                SubMeshDescriptor submesh = srcMesh.GetSubMesh(s);
                int start = submesh.indexStart;
                int end = start + submesh.indexCount;
                int j = end - 3;                
                for (int i = start; i < end; i += 3)
                {
                    reversed[j] = forward[i];
                    reversed[j + 1] = forward[i + 1];
                    reversed[j + 2] = forward[i + 2];
                    j -= 3;
                }
            }

            dstMesh.triangles = reversed;
            dstMesh.subMeshCount = srcMesh.subMeshCount;

            // second pass: copy sub-mesh data (vertex and triangle data must be present for this)
            for (int s = 0; s < srcMesh.subMeshCount; s++)
            {
                SubMeshDescriptor submesh = srcMesh.GetSubMesh(s);
                dstMesh.SetSubMesh(s, submesh);
            }

            // copy any blendshapes across
            if (srcMesh.blendShapeCount > 0)
            {
                Vector3[] bufVerts = new Vector3[srcMesh.vertexCount];
                Vector3[] bufNormals = new Vector3[srcMesh.vertexCount];
                Vector3[] bufTangents = new Vector3[srcMesh.vertexCount];

                for (int i = 0; i < srcMesh.blendShapeCount; i++)
                {
                    string name = srcMesh.GetBlendShapeName(i);

                    int frameCount = srcMesh.GetBlendShapeFrameCount(i);
                    for (int f = 0; f < frameCount; f++)
                    {
                        float frameWeight = srcMesh.GetBlendShapeFrameWeight(i, f);
                        srcMesh.GetBlendShapeFrameVertices(i, f, bufVerts, bufNormals, bufTangents);
                        dstMesh.AddBlendShapeFrame(name, frameWeight, bufVerts, bufNormals, bufTangents);                        
                    }
                }
            }

            // Save the mesh asset.
            if (!AssetDatabase.IsValidFolder(meshFolder))
                AssetDatabase.CreateFolder(folder, INVERTED_FOLDER_NAME);
            string meshPath = Path.Combine(meshFolder, srcObj.name + ".mesh");
            AssetDatabase.CreateAsset(dstMesh, meshPath);

            if (obj.GetType() == typeof(GameObject))
            {
                GameObject go = (GameObject)obj;
                if (go)
                {
                    Mesh createdMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

                    if (!ReplaceMesh(obj, createdMesh))
                    {
                        Debug.LogError("Unable to set mesh in selected object!");
                    }
                }
            }
        }

        public static GameObject FindCharacterBone(GameObject gameObject, string name)
        {
            if (gameObject)
            {
                if (gameObject.name.iEndsWith(name))
                    return gameObject;

                int children = gameObject.transform.childCount;
                for (int i = 0; i < children; i++)
                {
                    GameObject found = FindCharacterBone(gameObject.transform.GetChild(i).gameObject, name);
                    if (found) return found;
                }
            }

            return null;
        }

        public static void CharacterOpenCloseMouth(Object obj)
        {
            if (!obj) return;

            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(obj);

            if (root)
            {
                bool isOpen;

                // find the jaw bone and change it's rotation
                GameObject jawBone = FindCharacterBone(root, "CC_Base_JawRoot");
                if (!jawBone) jawBone = FindCharacterBone(root, "JawRoot");
                if (jawBone)
                {
                    Transform jaw = jawBone.transform;
                    Quaternion rotation = jaw.localRotation;
                    Vector3 euler = rotation.eulerAngles;
                    if (euler.z < 91f || euler.z > 269f)
                    {
                        euler.z = -108f;
                        isOpen = true;
                    }
                    else
                    {
                        euler.z = -90f;
                        isOpen = false;
                    }
                    rotation.eulerAngles = euler;
                    jaw.localRotation = rotation;

                    const string shapeName = "Mouth_Open";

                    // go through all the mesh object with blendshapes and set the "Mouth_Open" blend shape
                    for (int i = 0; i < root.transform.childCount; i++)
                    {
                        GameObject child = root.transform.GetChild(i).gameObject;
                        SkinnedMeshRenderer renderer = child.GetComponent<SkinnedMeshRenderer>();
                        if (renderer)
                        {
                            Mesh mesh = renderer.sharedMesh;
                            if (mesh.blendShapeCount > 0)
                            {
                                int shapeIndex = mesh.GetBlendShapeIndex(shapeName);
                                if (shapeIndex > 0)
                                {
                                    renderer.SetBlendShapeWeight(shapeIndex, isOpen ? 100f : 0f);
                                }
                            }
                        }
                    }
                }
            }
        }

        public static void CharacterOpenCloseEyes(Object obj)
        {
            if (!obj) return;

            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(obj);

            if (root)
            {
                bool isOpen;

                const string shapeNameL = "Eye_Blink_L";
                const string shapeNameR = "Eye_Blink_R";
                const string shapeNameSingle = "Eye_Blink";

                // go through all the mesh object with blendshapes and set the "Mouth_Open" blend shape
                for (int i = 0; i < root.transform.childCount; i++)
                {
                    GameObject child = root.transform.GetChild(i).gameObject;
                    SkinnedMeshRenderer renderer = child.GetComponent<SkinnedMeshRenderer>();
                    if (renderer)
                    {
                        Mesh mesh = renderer.sharedMesh;
                        if (mesh.blendShapeCount > 0)
                        {
                            int shapeIndexL = mesh.GetBlendShapeIndex(shapeNameL);
                            int shapeIndexR = mesh.GetBlendShapeIndex(shapeNameR);
                            int shapeIndexS = mesh.GetBlendShapeIndex(shapeNameSingle);

                            if (shapeIndexL > 0 && shapeIndexR > 0)
                            {
                                if (renderer.GetBlendShapeWeight(shapeIndexL) > 0f) isOpen = false;
                                else isOpen = true;

                                renderer.SetBlendShapeWeight(shapeIndexL, isOpen ? 100f : 0f);
                                renderer.SetBlendShapeWeight(shapeIndexR, isOpen ? 100f : 0f);
                            }
                            else if (shapeIndexS > 0)
                            {
                                if (renderer.GetBlendShapeWeight(shapeIndexS) > 0f) isOpen = false;
                                else isOpen = true;

                                renderer.SetBlendShapeWeight(shapeIndexS, isOpen ? 100f : 0f);                                
                            }
                        }
                    }
                }
            }
        }

        public enum EyeLookDir { None = 0, Left = 1, Right = 2, Up = 4, Down = 8 }
        public static void CharacterEyeLook(Object obj, EyeLookDir dirFlags)
        {
            if (!obj) return;

            GameObject root = PrefabUtility.GetOutermostPrefabInstanceRoot(obj);

            if (root)
            {
                GameObject leftEye = FindCharacterBone(root, "CC_Base_L_Eye");
                if (!leftEye) leftEye = FindCharacterBone(root, "L_Eye");
                GameObject rightEye = FindCharacterBone(root, "CC_Base_R_Eye");
                if (!rightEye) rightEye = FindCharacterBone(root, "R_Eye");

                if (leftEye && rightEye)
                {
                    Vector3 euler;

                    if (dirFlags == 0) euler = new Vector3(0, -90f, 180f);
                    else euler = leftEye.transform.localRotation.eulerAngles;

                    if ((dirFlags & EyeLookDir.Left) > 0) euler.z = 168f;
                    if ((dirFlags & EyeLookDir.Right) > 0) euler.z = 192f;
                    if ((dirFlags & EyeLookDir.Up) > 0) euler.x = 10f;
                    if ((dirFlags & EyeLookDir.Down) > 0) euler.x = -10f;

                    Quaternion rotation = Quaternion.identity;
                    rotation.eulerAngles = euler;
                    leftEye.transform.localRotation = rotation;
                    rightEye.transform.localRotation = rotation;
                }
            }
        }

        /*
        [MenuItem("CC3/Tools/Setup Dual Material Hair", priority = 700)]
        private static void DoEHM()
        {
            MeshUtil.Extract2PassHairMeshes(Selection.activeObject);
        }        
        */

        public static Mesh ExtractSubMesh(Mesh srcMesh, int index)
        {
            SubMeshDescriptor extractMeshDesc = srcMesh.GetSubMesh(index);

            // operate on a local copy of the source mesh data (much faster)
            Vector3[] srcVertices = srcMesh.vertices;
            Vector2[] srcUv = srcMesh.uv;
            Vector2[] srcUv2 = srcMesh.uv2;
            Vector2[] srcUv3 = srcMesh.uv3;
            Vector2[] srcUv4 = srcMesh.uv4;
            Vector2[] srcUv5 = srcMesh.uv5;
            Vector2[] srcUv6 = srcMesh.uv6;
            Vector2[] srcUv7 = srcMesh.uv7;
            Vector2[] srcUv8 = srcMesh.uv8;
            Vector3[] srcNormals = srcMesh.normals;
            Color[] srcColors = srcMesh.colors;
            BoneWeight[] srcBoneWeights = srcMesh.boneWeights;
            Vector4[] srcTangents = srcMesh.tangents;
            int[] srcTriangles = srcMesh.triangles;

            // first determine which vertices are used in the faces of the indexed submesh and remap their indices to the new mesh.
            int maxVerts = srcMesh.vertexCount;
            int[] remapping = new int[maxVerts];
            for (int i = 0; i < maxVerts; i++) remapping[i] = -1;
            int pointer = 0;            
            for (int tIndex = extractMeshDesc.indexStart; tIndex < extractMeshDesc.indexStart + extractMeshDesc.indexCount; tIndex++)
            {
                int vertIndex = srcTriangles[tIndex];
                if (remapping[vertIndex] == -1) remapping[vertIndex] = pointer++;
            }
            // this also tells us how many vertices are in the sub-mesh.
            int numNewVerts = pointer;

            // now create the extracted mesh
            Mesh newMesh = new Mesh();
            Vector3[] vertices = new Vector3[numNewVerts];
            Vector2[] uv = new Vector2[srcUv.Length > 0 ? numNewVerts : 0];
            Vector2[] uv2 = new Vector2[srcUv2.Length > 0 ? numNewVerts : 0];
            Vector2[] uv3 = new Vector2[srcUv3.Length > 0 ? numNewVerts : 0];
            Vector2[] uv4 = new Vector2[srcUv4.Length > 0 ? numNewVerts : 0];
            Vector2[] uv5 = new Vector2[srcUv5.Length > 0 ? numNewVerts : 0];
            Vector2[] uv6 = new Vector2[srcUv6.Length > 0 ? numNewVerts : 0];
            Vector2[] uv7 = new Vector2[srcUv7.Length > 0 ? numNewVerts : 0];
            Vector2[] uv8 = new Vector2[srcUv8.Length > 0 ? numNewVerts : 0];
            Vector3[] normals = new Vector3[srcNormals.Length > 0 ? numNewVerts : 0];
            Color[] colors = new Color[srcColors.Length > 0 ? numNewVerts : 0];
            BoneWeight[] boneWeights = new BoneWeight[srcBoneWeights.Length > 0 ? numNewVerts : 0];
            Vector4[] tangents = new Vector4[srcTangents.Length > 0 ? numNewVerts : 0];            
            // copy and remap all the submesh vert data into the new mesh
            for (int vertIndex = 0; vertIndex < maxVerts; vertIndex++)
            {                
                int remappedIndex = remapping[vertIndex];
                if (remappedIndex >= 0)
                {
                    vertices[remappedIndex] = srcVertices[vertIndex];
                    if (srcUv.Length > 0)
                        uv[remappedIndex] = srcUv[vertIndex];
                    if (srcUv2.Length > 0)
                        uv2[remappedIndex] = srcUv2[vertIndex];
                    if (srcUv3.Length > 0)
                        uv3[remappedIndex] = srcUv3[vertIndex];
                    if (srcUv4.Length > 0)
                        uv4[remappedIndex] = srcUv4[vertIndex];
                    if (srcUv5.Length > 0)
                        uv5[remappedIndex] = srcUv5[vertIndex];
                    if (srcUv6.Length > 0)
                        uv6[remappedIndex] = srcUv6[vertIndex];
                    if (srcUv7.Length > 0)
                        uv7[remappedIndex] = srcUv7[vertIndex];
                    if (srcUv8.Length > 0)
                        uv8[remappedIndex] = srcUv8[vertIndex];
                    if (srcNormals.Length >0)
                        normals[remappedIndex] = srcNormals[vertIndex];
                    if (srcColors.Length > 0)
                        colors[remappedIndex] = srcColors[vertIndex];
                    if (srcBoneWeights.Length > 0)
                        boneWeights[remappedIndex] = srcBoneWeights[vertIndex];
                    if (srcBoneWeights.Length > 0)
                        tangents[remappedIndex] = srcTangents[vertIndex];
                }
            }            
            newMesh.vertices = vertices;
            newMesh.uv = uv;
            newMesh.uv2 = uv2;
            newMesh.uv3 = uv3;
            newMesh.uv4 = uv4;
            newMesh.uv5 = uv5;
            newMesh.uv6 = uv6;
            newMesh.uv7 = uv7;
            newMesh.uv8 = uv8;
            newMesh.normals = normals;
            newMesh.colors = colors;
            newMesh.boneWeights = boneWeights;
            newMesh.tangents = tangents;
            newMesh.bindposes = srcMesh.bindposes;
            newMesh.bounds = srcMesh.bounds;
            newMesh.subMeshCount = 1;
            // finally copy and remap the triangle data last 
            int[] triangles = new int[extractMeshDesc.indexCount];
            pointer = 0;
            for (int tIndex = extractMeshDesc.indexStart; tIndex < extractMeshDesc.indexStart + extractMeshDesc.indexCount; tIndex++)
            {
                int vertIndex = srcTriangles[tIndex];
                int remappedIndex = remapping[vertIndex];
                if (remappedIndex >= 0)
                    triangles[pointer++] = remappedIndex;
            }
            newMesh.triangles = triangles;
            // copy any blendshapes across
            if (srcMesh.blendShapeCount > 0)
            {
                // source buffer for blend shapes
                Vector3[] bufVerts = new Vector3[srcMesh.vertexCount];
                Vector3[] bufNormals = new Vector3[srcMesh.vertexCount];
                Vector3[] bufTangents = new Vector3[srcMesh.vertexCount];

                // frame buffer for adding blend shapes to new mesh
                Vector3[] frameVerts = new Vector3[numNewVerts];
                Vector3[] frameNormals = new Vector3[numNewVerts];
                Vector3[] frameTangents = new Vector3[numNewVerts];

                for (int i = 0; i < srcMesh.blendShapeCount; i++)
                {
                    string name = srcMesh.GetBlendShapeName(i);

                    int frameCount = srcMesh.GetBlendShapeFrameCount(i);
                    for (int f = 0; f < frameCount; f++)
                    {
                        float frameWeight = srcMesh.GetBlendShapeFrameWeight(i, f);
                        srcMesh.GetBlendShapeFrameVertices(i, f, bufVerts, bufNormals, bufTangents);
                        for (int vertIndex = 0; vertIndex < maxVerts; vertIndex++)
                        {
                            int remappedIndex = remapping[vertIndex];
                            if (remappedIndex >= 0)
                            {
                                frameVerts[remappedIndex] = bufVerts[vertIndex];
                                frameNormals[remappedIndex] = bufNormals[vertIndex];
                                frameTangents[remappedIndex] = bufTangents[vertIndex];
                            }
                        }
                        newMesh.AddBlendShapeFrame(name, frameWeight, frameVerts, frameNormals, frameTangents);
                    }
                }
            }
            SubMeshDescriptor newMeshDesc = extractMeshDesc;
            newMeshDesc.firstVertex = 0;
            newMeshDesc.indexStart = 0;
            newMeshDesc.indexCount = extractMeshDesc.indexCount;
            newMeshDesc.vertexCount = numNewVerts;            
            newMesh.SetSubMesh(0, newMeshDesc);

            return newMesh;
        }

        public static Mesh RemoveSubMeshes(Mesh srcMesh, List<int> indices)
        {            
            // operate on a local copy of the source mesh data (much faster)
            Vector3[] srcVertices = srcMesh.vertices;
            Vector2[] srcUv = srcMesh.uv;
            Vector2[] srcUv2 = srcMesh.uv2;
            Vector2[] srcUv3 = srcMesh.uv3;
            Vector2[] srcUv4 = srcMesh.uv4;
            Vector2[] srcUv5 = srcMesh.uv5;
            Vector2[] srcUv6 = srcMesh.uv6;
            Vector2[] srcUv7 = srcMesh.uv7;
            Vector2[] srcUv8 = srcMesh.uv8;
            Vector3[] srcNormals = srcMesh.normals;
            Color[] srcColors = srcMesh.colors;
            BoneWeight[] srcBoneWeights = srcMesh.boneWeights;
            Vector4[] srcTangents = srcMesh.tangents;
            int[] srcTriangles = srcMesh.triangles;

            // first determine which vertices are used in the faces of *ALL SUBMESHES EXCEPT* the indexed submesh 
            // and remap their indices to the new mesh.
            int maxVerts = srcMesh.vertexCount;
            int[] remapping = new int[maxVerts];
            for (int i = 0; i < maxVerts; i++) remapping[i] = -1;
            int pointer = 0;
            int numNewTriangles = 0;
            for (int s = 0; s < srcMesh.subMeshCount; s++)
            {
                if (!indices.Contains(s))
                {
                    SubMeshDescriptor meshDesc = srcMesh.GetSubMesh(s);
                    numNewTriangles += meshDesc.indexCount;
                    for (int tIndex = meshDesc.indexStart; tIndex < meshDesc.indexStart + meshDesc.indexCount; tIndex++)
                    {
                        int vertIndex = srcTriangles[tIndex];
                        if (remapping[vertIndex] == -1) remapping[vertIndex] = pointer++;
                    }
                }
            }
            // this also tells us how many vertices are in the new mesh.
            int numNewVerts = pointer;

            // now create the extracted mesh
            Mesh newMesh = new Mesh();
            Vector3[] vertices = new Vector3[numNewVerts];
            Vector2[] uv = new Vector2[srcUv.Length > 0 ? numNewVerts : 0];
            Vector2[] uv2 = new Vector2[srcUv2.Length > 0 ? numNewVerts : 0];
            Vector2[] uv3 = new Vector2[srcUv3.Length > 0 ? numNewVerts : 0];
            Vector2[] uv4 = new Vector2[srcUv4.Length > 0 ? numNewVerts : 0];
            Vector2[] uv5 = new Vector2[srcUv5.Length > 0 ? numNewVerts : 0];
            Vector2[] uv6 = new Vector2[srcUv6.Length > 0 ? numNewVerts : 0];
            Vector2[] uv7 = new Vector2[srcUv7.Length > 0 ? numNewVerts : 0];
            Vector2[] uv8 = new Vector2[srcUv8.Length > 0 ? numNewVerts : 0];
            Vector3[] normals = new Vector3[srcNormals.Length > 0 ? numNewVerts : 0];
            Color[] colors = new Color[srcColors.Length > 0 ? numNewVerts : 0];
            BoneWeight[] boneWeights = new BoneWeight[srcBoneWeights.Length > 0 ? numNewVerts : 0];
            Vector4[] tangents = new Vector4[srcTangents.Length > 0 ? numNewVerts : 0];            
            // copy and remap all the submesh vert data into the new mesh
            for (int vertIndex = 0; vertIndex < maxVerts; vertIndex++)
            {
                int remappedIndex = remapping[vertIndex];
                if (remappedIndex >= 0)
                {
                    vertices[remappedIndex] = srcVertices[vertIndex];
                    if (srcUv.Length > 0)
                        uv[remappedIndex] = srcUv[vertIndex];
                    if (srcUv2.Length > 0)
                        uv2[remappedIndex] = srcUv2[vertIndex];
                    if (srcUv3.Length > 0)
                        uv3[remappedIndex] = srcUv3[vertIndex];
                    if (srcUv4.Length > 0)
                        uv4[remappedIndex] = srcUv4[vertIndex];
                    if (srcUv5.Length > 0)
                        uv5[remappedIndex] = srcUv5[vertIndex];
                    if (srcUv6.Length > 0)
                        uv6[remappedIndex] = srcUv6[vertIndex];
                    if (srcUv7.Length > 0)
                        uv7[remappedIndex] = srcUv7[vertIndex];
                    if (srcUv8.Length > 0)
                        uv8[remappedIndex] = srcUv8[vertIndex];
                    if (srcNormals.Length >0)
                        normals[remappedIndex] = srcNormals[vertIndex];
                    if (srcColors.Length > 0)
                        colors[remappedIndex] = srcColors[vertIndex];
                    if (srcBoneWeights.Length > 0)
                        boneWeights[remappedIndex] = srcBoneWeights[vertIndex];
                    if (srcBoneWeights.Length > 0)
                        tangents[remappedIndex] = srcTangents[vertIndex];
                }
            }
            newMesh.vertices = vertices;
            newMesh.uv = uv;
            newMesh.uv2 = uv2;
            newMesh.normals = normals;
            newMesh.colors = colors;
            newMesh.boneWeights = boneWeights;
            newMesh.tangents = tangents;
            newMesh.bindposes = srcMesh.bindposes;
            newMesh.bounds = srcMesh.bounds;            
            // finally copy and remap the triangle data last
            int[] triangles = new int[numNewTriangles];
            pointer = 0;
            for (int tIndex = 0; tIndex < srcTriangles.Length; tIndex++)
            {
                int vertIndex = srcTriangles[tIndex];
                int remappedIndex = remapping[vertIndex];
                if (remappedIndex >= 0)
                    triangles[pointer++] = remappedIndex;
            }
            newMesh.triangles = triangles;
            // copy any blendshapes across
            if (srcMesh.blendShapeCount > 0)
            {
                // source buffer for blend shapes
                Vector3[] bufVerts = new Vector3[srcMesh.vertexCount];
                Vector3[] bufNormals = new Vector3[srcMesh.vertexCount];
                Vector3[] bufTangents = new Vector3[srcMesh.vertexCount];

                // frame buffer for adding blend shapes to new mesh
                Vector3[] frameVerts = new Vector3[numNewVerts];
                Vector3[] frameNormals = new Vector3[numNewVerts];
                Vector3[] frameTangents = new Vector3[numNewVerts];

                for (int i = 0; i < srcMesh.blendShapeCount; i++)
                {
                    string name = srcMesh.GetBlendShapeName(i);

                    int frameCount = srcMesh.GetBlendShapeFrameCount(i);
                    for (int f = 0; f < frameCount; f++)
                    {
                        float frameWeight = srcMesh.GetBlendShapeFrameWeight(i, f);
                        srcMesh.GetBlendShapeFrameVertices(i, f, bufVerts, bufNormals, bufTangents);
                        for (int vertIndex = 0; vertIndex < maxVerts; vertIndex++)
                        {
                            int remappedIndex = remapping[vertIndex];
                            if (remappedIndex >= 0)
                            {
                                frameVerts[remappedIndex] = bufVerts[vertIndex];
                                frameNormals[remappedIndex] = bufNormals[vertIndex];
                                frameTangents[remappedIndex] = bufTangents[vertIndex];
                            }
                        }
                        newMesh.AddBlendShapeFrame(name, frameWeight, frameVerts, frameNormals, frameTangents);
                    }
                }
            }

            pointer = 0;
            int indexStart = 0;
            newMesh.subMeshCount = srcMesh.subMeshCount - 1;
            for (int s = 0; s < srcMesh.subMeshCount; s++)
            {
                if (!indices.Contains(s))
                {
                    SubMeshDescriptor meshDesc = srcMesh.GetSubMesh(s);
                    SubMeshDescriptor newMeshDesc = meshDesc;
                    newMeshDesc.firstVertex = remapping[meshDesc.firstVertex];
                    newMeshDesc.indexStart = indexStart;
                    newMeshDesc.indexCount = meshDesc.indexCount;
                    newMeshDesc.vertexCount = meshDesc.vertexCount;                    
                    newMesh.SetSubMesh(pointer++, newMeshDesc);
                    indexStart += meshDesc.indexCount;
                }
            }            

            return newMesh;
        }

        public static void CopyMaterialParameters(Material from, Material to)
        {
            to.CopyPropertiesFromMaterial(from);            
        }

        private static void FixHDRP2PassMaterials(Material firstPass, Material secondPass)
        {
            if (Pipeline.isHDRP)
            {
                firstPass.SetFloat("_SurfaceType", 0f);
                firstPass.SetFloat("_ENUMCLIPQUALITY_ON", 0f);
                Pipeline.ResetMaterial(firstPass);

                secondPass.SetFloat("_SurfaceType", 1f);
                secondPass.SetFloat("_AlphaCutoffEnable", 0f);
                secondPass.SetFloat("_TransparentDepthPostpassEnable", 0f);
                secondPass.SetFloat("_TransparentDepthPrepassEnable", 0f);
                secondPass.SetFloat("_EnableBlendModePreserveSpecularLighting", 0f);
                secondPass.SetFloat("_ZTestDepthEqualForOpaque", 2f);
                secondPass.SetFloat("_ZTestTransparent", 2f);
                secondPass.SetFloat("_ENUMCLIPQUALITY_ON", 0f);
                Pipeline.ResetMaterial(secondPass);
            }
        }

        public static void Extract2PassHairMeshes(Object obj)
        {
            if (!obj) return;            
            GameObject sceneRoot = PrefabUtility.GetOutermostPrefabInstanceRoot(obj);
            if (!sceneRoot) sceneRoot = (GameObject)obj;
            GameObject fbxAsset = Util.GetRootPrefabFromObject(sceneRoot);
            GameObject prefab = Util.GetCharacterPrefab(fbxAsset);
            string fbxPath = AssetDatabase.GetAssetPath(fbxAsset);
            string name = Path.GetFileNameWithoutExtension(fbxPath);
            string fbxFolder = Path.GetDirectoryName(fbxPath);
            string materialFolder = Path.Combine(fbxFolder, Importer.MATERIALS_FOLDER, name);
            string meshFolder = Path.Combine(fbxFolder, MESH_FOLDER_NAME, name);            

            if (!prefab) return;
            
            GameObject clone = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            int processCount = 0;

            Renderer[] renderers = clone.GetComponentsInChildren<Renderer>();

            foreach (Renderer r in renderers)
            {
                bool hasHairMaterial = false;
                int subMeshCount = 0;
                foreach (Material m in r.sharedMaterials)
                {
                    subMeshCount++;
                    if (m.shader.name.iEndsWith(Pipeline.SHADER_HQ_HAIR))
                        hasHairMaterial = true;
                }

                if (hasHairMaterial)
                {
                    List<int> indicesToRemove = new List<int>();
                    bool dontRemoveMaterials = false;

                    GameObject oldObj = r.gameObject;
                    Mesh oldMesh = GetMeshFrom(oldObj);
                    SkinnedMeshRenderer oldSmr = oldObj.GetComponent<SkinnedMeshRenderer>();

                    for (int index = 0; index < r.sharedMaterials.Length; index++)
                    {
                        Material oldMat = r.sharedMaterials[index];

                        if (oldMat.shader.name.iEndsWith(Pipeline.SHADER_HQ_HAIR))
                        {
                            // set alpha clip and remap to values that work better 
                            // with the two material system.
                            oldMat.SetFloatIf("_AlphaClip", 0.5f);
                            oldMat.SetFloatIf("_AlphaClip2", 0.5f);
                            oldMat.SetFloatIf("_AlphaPower", 1.0f);
                            oldMat.SetFloatIf("_AlphaRemap", 1.0f);
                        }

                        if (subMeshCount > 1 && oldMat.shader.name.iEndsWith(Pipeline.SHADER_HQ_HAIR))
                        {                            
                            Debug.Log("Extracting subMesh(" + index.ToString() +  ") from Object: " + oldObj.name);

                            // extract mesh into two new meshes, the old mesh without the extracted submesh
                            // and just the extracted submesh
                            Mesh newMesh = ExtractSubMesh(oldMesh, index);
                            // Save the mesh asset.
                            Util.EnsureAssetsFolderExists(meshFolder);
                            string meshPath = Path.Combine(meshFolder, oldObj.name + "_ExtractedHairMesh" + index.ToString() + ".mesh");
                            AssetDatabase.CreateAsset(newMesh, meshPath);
                            newMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

                            // add new object as sibling to old object:
                            GameObject newObj = new GameObject();
                            newObj.name = oldObj.name + "_Extracted" + index.ToString();
                            newObj.transform.parent = oldObj.transform.parent;
                            newObj.transform.localPosition = oldObj.transform.localPosition;
                            newObj.transform.localRotation = oldObj.transform.localRotation;
                            newObj.transform.localScale = oldObj.transform.localScale;
                            SkinnedMeshRenderer smr = newObj.AddComponent<SkinnedMeshRenderer>();
                            smr.localBounds = oldSmr.localBounds;
                            smr.quality = oldSmr.quality;
                            smr.rootBone = oldSmr.rootBone;
                            smr.bones = oldSmr.bones;

                            // - set skinnedMeshRenderer mesh to extracted mesh
                            smr.sharedMesh = newMesh;
                            Material[] sharedMaterials = new Material[2];
                            // - add first pass hair shader material
                            // - add second pass hair shader material
                            Material firstPassTemplate = Util.FindMaterial(Pipeline.MATERIAL_HQ_HAIR_1ST_PASS);
                            Material secondPassTemplate = Util.FindMaterial(Pipeline.MATERIAL_HQ_HAIR_2ND_PASS);
                            Material firstPass = new Material(firstPassTemplate);
                            Material secondPass = new Material(secondPassTemplate);
                            CopyMaterialParameters(oldMat, firstPass);
                            CopyMaterialParameters(oldMat, secondPass);
                            FixHDRP2PassMaterials(firstPass, secondPass);
                            // save the materials to the asset database.
                            AssetDatabase.CreateAsset(firstPass, Path.Combine(materialFolder, oldMat.name + "_1st_Pass.mat"));
                            AssetDatabase.CreateAsset(secondPass, Path.Combine(materialFolder, oldMat.name + "_2nd_Pass.mat"));
                            sharedMaterials[0] = firstPass;
                            sharedMaterials[1] = secondPass;
                            // add the 1st and 2nd pass materials to the mesh renderer
                            // a single submesh with multiple materials will render itself again with each material
                            // effectively acting as a multi-pass shader which fully complies with any SRP batching.
                            smr.sharedMaterials = sharedMaterials;

                            indicesToRemove.Add(index);
                            subMeshCount--;
                            processCount++;
                        }
                        else if (subMeshCount == 1 && oldMat.shader.name.iEndsWith(Pipeline.SHADER_HQ_HAIR))
                        {
                            Debug.Log("Leaving subMesh(" + index.ToString() + ") in Object: " + oldObj.name);

                            Material[] sharedMaterials = new Material[2];
                            // - add first pass hair shader material
                            // - add second pass hair shader material
                            Material firstPassTemplate = Util.FindMaterial(Pipeline.MATERIAL_HQ_HAIR_1ST_PASS);
                            Material secondPassTemplate = Util.FindMaterial(Pipeline.MATERIAL_HQ_HAIR_2ND_PASS);
                            Material firstPass = new Material(firstPassTemplate);
                            Material secondPass = new Material(secondPassTemplate);
                            CopyMaterialParameters(oldMat, firstPass);
                            CopyMaterialParameters(oldMat, secondPass);
                            FixHDRP2PassMaterials(firstPass, secondPass);
                            // save the materials to the asset database.
                            AssetDatabase.CreateAsset(firstPass, Path.Combine(materialFolder, oldMat.name + "_1st_Pass.mat"));
                            AssetDatabase.CreateAsset(secondPass, Path.Combine(materialFolder, oldMat.name + "_2nd_Pass.mat"));
                            sharedMaterials[0] = firstPass;
                            sharedMaterials[1] = secondPass;
                            // add the 1st and 2nd pass materials to the mesh renderer
                            // a single submesh with multiple materials will render itself again with each material
                            // effectively acting as a multi-pass shader which fully complies with any SRP batching.
                            oldSmr.sharedMaterials = sharedMaterials;
                            // as we have replaced the materials completely, don't remove any later when removing any submeshes...
                            dontRemoveMaterials = true;
                            processCount++;
                        }                        
                    }

                    if (indicesToRemove.Count > 0)
                    {
                        Debug.Log("Removing submeshes from Object: " + oldObj.name);
                        Mesh remainingMesh = RemoveSubMeshes(oldMesh, indicesToRemove);
                        // Save the mesh asset.                        
                        string meshPath = Path.Combine(meshFolder, oldObj.name + "_Remaining.mesh");
                        AssetDatabase.CreateAsset(remainingMesh, meshPath);
                        remainingMesh = AssetDatabase.LoadAssetAtPath<Mesh>(meshPath);

                        // replace mesh in obj.skinnedMeshRenderer with remaining submeshes
                        oldSmr.sharedMesh = remainingMesh;

                        if (!dontRemoveMaterials)
                        {
                            // remove old hair material from old shared material list...
                            Material[] sharedMaterials = new Material[oldSmr.sharedMaterials.Length - indicesToRemove.Count];
                            int i = 0;
                            for (int j = 0; j < oldSmr.sharedMaterials.Length; j++)
                                if (!indicesToRemove.Contains(j))
                                    sharedMaterials[i++] = oldSmr.sharedMaterials[j];
                            oldSmr.sharedMaterials = sharedMaterials;
                        }

                        processCount++;
                    }
                }
            }

            if (prefab && processCount > 0)
            {                
                Debug.Log("Updating character prefab...");
                // save the clone as the prefab for this character         
                string prefabPath = AssetDatabase.GetAssetPath(prefab);
                prefab = PrefabUtility.SaveAsPrefabAsset(clone, prefabPath);                
                UnityEngine.Object.DestroyImmediate(clone);
            }
            else
            {
                Debug.Log("Nothing to process (or already processed)...");
            }

            if (clone) UnityEngine.Object.DestroyImmediate(clone);
        }
    }
}
