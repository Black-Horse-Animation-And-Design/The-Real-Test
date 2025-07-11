/*
Bake AO - Easy Ambient Occlusion Baking - A plugin for baking ambient occlusion (AO) textures in the Unity Editor.
by Procedural Pixels - Jan Mróz

Documentation: https://proceduralpixels.com/BakeAO/Documentation
Asset Store: https://assetstore.unity.com/packages/slug/263743 

Help: If the plugin is not working correctly, if there’s a bug, or if you need assistance and the documentation does not help, please contact me via Discord (https://discord.gg/NT2pyQ28Jx) or email (dev@proceduralpixels.com).
*/

using UnityEditor;
using UnityEngine;

namespace ProceduralPixels.BakeAO.Editor
{
    [System.Serializable]
    internal struct SerializableMeshContext
    {
        public string meshGUID;
        public long meshFileID;
        public UVChannel uv;
        public Matrix4x4 objectToWorld;
        public float uvToWorldRatio;
        public long submeshFlags; // -1 and 0 bake all the submeshes.
        public float transparency;

        public MeshContextUseFlags useFlags;
        public Mesh mesh;

        public SerializableMeshContext(SerializableMeshContext other)
        {
            meshGUID = other.meshGUID;
            meshFileID = other.meshFileID;
            uv = other.uv;
            objectToWorld = other.objectToWorld;
            uvToWorldRatio = other.uvToWorldRatio;
            submeshFlags = other.submeshFlags;
            useFlags = other.useFlags;
            mesh = other.mesh;
            transparency = other.transparency;
        }

        public SerializableMeshContext(MeshContext meshContext) : this(meshContext.mesh, meshContext.submeshFlags, meshContext.uv, meshContext.objectToWorld, meshContext.uvToWorldRatio)
        {
            useFlags |= meshContext.useFlags;
            transparency = meshContext.transparency;
        }

        public SerializableMeshContext(Mesh mesh, long submeshFlags, UVChannel uv, Matrix4x4 objectToWorld, float uvToWorldRatio)
        {
            this.uv = uv;
            this.objectToWorld = objectToWorld;
            this.mesh = mesh;
            this.submeshFlags = (submeshFlags == 0) ? -1 : submeshFlags;
            this.useFlags = MeshContextUseFlags.Default;
            if (mesh != null)
                this.useFlags |= (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out meshGUID, out meshFileID)) ? MeshContextUseFlags.IsTemporary : MeshContextUseFlags.Default;
            else
            {
                meshGUID = "";
                meshFileID = 0;
            }
            this.uvToWorldRatio = uvToWorldRatio;
            this.transparency = 0.0f;
        }

        public Mesh GetMesh()
        {
            AssetDatabaseUtils.TryGetObjectFromGUIDAndLocalFileIdentifier(meshGUID, meshFileID, out Mesh mesh);
            return mesh;
        }

        public MeshContext GetMeshContext()
        {
            MeshContext context = new MeshContext();
            context.uv = uv;
            context.objectToWorld = objectToWorld;
            context.useFlags = useFlags;
            context.uvToWorldRatio = uvToWorldRatio;
            context.submeshFlags = submeshFlags;
            context.transparency = transparency;

            if (AssetDatabaseUtils.TryGetObjectFromGUIDAndLocalFileIdentifier(meshGUID, meshFileID, out Mesh serializedMesh))
                context.mesh = serializedMesh;
            else if (this.mesh != null)
                context.mesh = this.mesh;

            return context;
        }

        public void SerializeTemporaryMesh()
        {
            if (!useFlags.HasFlag(MeshContextUseFlags.IsTemporary))
                return;

            Mesh meshToSerialize;
            //if (useFlags.HasFlag(MeshContextUseFlags.IsNotUsedInAnyAsset))
            //    meshToSerialize = mesh;
            //else
            meshToSerialize = BakeAOUtils.CloneMesh(mesh);

            BakeAOTemporaryData.SerializeAndAddTemporaryMesh(meshToSerialize);
            this.mesh = meshToSerialize;
            this.useFlags |= (!AssetDatabase.TryGetGUIDAndLocalFileIdentifier(mesh, out meshGUID, out meshFileID)) ? MeshContextUseFlags.IsTemporary : MeshContextUseFlags.Default;
        }
    }
}