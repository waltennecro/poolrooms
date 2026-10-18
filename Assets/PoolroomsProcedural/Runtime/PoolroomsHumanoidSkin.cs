using System;
using System.Collections.Generic;
using Poolrooms.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace Poolrooms
{
    // One renderer skeleton and one pose source. This component is explicitly ticked by its owner.
    public sealed class PoolroomsHumanoidSkin : MonoBehaviour
    {
        public HumanoidRig Rig {get;private set;}
        public string Report {get;private set;}
        public readonly HumanoidAnimation Motion=new HumanoidAnimation();
        readonly Transform[] bones=new Transform[HumanoidRig.Count];
        readonly Quaternion[] restRotations=new Quaternion[HumanoidRig.Count];
        readonly List<Mesh> ownedMeshes=new List<Mesh>();
        readonly RaycastHit[] groundHits=new RaycastHit[16];
        float leftGround,rightGround,nextGround;
        static readonly HumanBodyBones[] mapping={HumanBodyBones.Hips,HumanBodyBones.Spine,HumanBodyBones.Chest,HumanBodyBones.Neck,HumanBodyBones.Head,
            HumanBodyBones.LeftShoulder,HumanBodyBones.LeftUpperArm,HumanBodyBones.LeftLowerArm,HumanBodyBones.LeftHand,
            HumanBodyBones.RightShoulder,HumanBodyBones.RightUpperArm,HumanBodyBones.RightLowerArm,HumanBodyBones.RightHand,
            HumanBodyBones.LeftUpperLeg,HumanBodyBones.LeftLowerLeg,HumanBodyBones.LeftFoot,HumanBodyBones.LeftToes,
            HumanBodyBones.RightUpperLeg,HumanBodyBones.RightLowerLeg,HumanBodyBones.RightFoot,HumanBodyBones.RightToes};
        public void Build(SkinPreferences preferences,float height)
        {
            preferences.Sanitize();var animator=GetComponentInChildren<Animator>(true);
            if(animator!=null&&animator.avatar!=null&&animator.isHuman)
            {
                Rig=new HumanoidRig(height,HumanoidRestPose.A);bool valid=true;
                for(int i=0;i<bones.Length;i++)
                {
                    bones[i]=animator.GetBoneTransform(mapping[i]);
                    if(bones[i]!=null)Rig.Rest[i]=ToCore(transform.InverseTransformPoint(bones[i].position));
                    else if(i==0||i==4||i==6||i==7||i==8||i==10||i==11||i==12||i==13||i==14||i==15||i==17||i==18||i==19)valid=false;
                }
                if(!valid)throw new FormatException("El Avatar Humanoid tiene huesos esenciales sin asignar. Corrige su Rig en Unity.");
                foreach(var a in GetComponentsInChildren<Animator>(true))a.enabled=false;
                foreach(var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))renderer.updateWhenOffscreen=true;
                CacheRest();Report="Humanoid existente · pesos conservados · animación común";Motion.Step(Rig,new HumanoidMotion{Grounded=true},.001f);return;
            }
            // Bake an unrecognized skin once, preserving all materials; never replace a valid Humanoid's weights.
            var source=new List<(Mesh mesh,Matrix4x4 matrix,Material[] materials,Renderer renderer)>();var points=new List<V3>();
            foreach(var filter in GetComponentsInChildren<MeshFilter>(true))
            {
                var renderer=filter.GetComponent<MeshRenderer>();if(filter.sharedMesh==null||renderer==null||!renderer.enabled)continue;
                if(!filter.sharedMesh.isReadable)throw new FormatException("Activa Read/Write en el modelo para calcular sus pesos, o impórtalo como Humanoid.");
                source.Add((filter.sharedMesh,transform.worldToLocalMatrix*filter.transform.localToWorldMatrix,renderer.sharedMaterials,renderer));
            }
            foreach(var renderer in GetComponentsInChildren<SkinnedMeshRenderer>(true))
            {
                if(renderer.sharedMesh==null||!renderer.enabled)continue;
                var baked=new Mesh{name="Avatar rest bake"};ownedMeshes.Add(baked);renderer.BakeMesh(baked);
                source.Add((baked,transform.worldToLocalMatrix*renderer.transform.localToWorldMatrix,renderer.sharedMaterials,renderer));
            }
            int total=0;foreach(var part in source)
            {
                total+=part.mesh.vertexCount;if(total>AvatarObj.MaxVertices)throw new FormatException("La adaptación automática admite 150.000 vértices en total. Usa LOD/modelo más ligero o Humanoid preparado.");
                foreach(var point in part.mesh.vertices)points.Add(ToCore(part.matrix.MultiplyPoint3x4(point)));
            }
            Rig=HumanoidRig.Fit(points,preferences);
            for(int i=0;i<bones.Length;i++)
            {
                var joint=new GameObject(((HumanoidBone)i).ToString()).transform;joint.SetParent(HumanoidRig.Parents[i]<0?transform:bones[HumanoidRig.Parents[i]],false);
                joint.position=transform.TransformPoint(ToUnity(Rig.Rest[i]));joint.rotation=transform.rotation;bones[i]=joint;
            }
            var replacements=new Dictionary<Renderer,Renderer>();
            foreach(var part in source)
            {
                var mesh=Instantiate(part.mesh);mesh.name=part.mesh.name+" / shared humanoid weights";ownedMeshes.Add(mesh);
                var vertices=mesh.vertices;var weights=new BoneWeight[vertices.Length];
                V3 centre=new V3();foreach(var p in vertices)centre+=ToCore(part.matrix.MultiplyPoint3x4(p));centre*=1f/Math.Max(1,vertices.Length);
                HumanoidWeight rigid=Rig.Weight(centre,true);
                for(int i=0;i<vertices.Length;i++)
                {
                    var w=preferences.rigidPieces?rigid:Rig.Weight(ToCore(part.matrix.MultiplyPoint3x4(vertices[i])));weights[i]=new BoneWeight{boneIndex0=w.A,boneIndex1=w.B,boneIndex2=w.C,boneIndex3=w.D,weight0=w.WA,weight1=w.WB,weight2=w.WC,weight3=w.WD};
                }
                var bindposes=new Matrix4x4[bones.Length];for(int i=0;i<bindposes.Length;i++)bindposes[i]=bones[i].worldToLocalMatrix*part.renderer.localToWorldMatrix;
                mesh.boneWeights=weights;mesh.bindposes=bindposes;mesh.RecalculateBounds();
                var go=new GameObject(part.renderer.name+" / animated skin");go.transform.SetParent(part.renderer.transform,false);
                var output=go.AddComponent<SkinnedMeshRenderer>();output.sharedMesh=mesh;output.sharedMaterials=part.materials;output.bones=bones;output.rootBone=bones[0];output.quality=SkinQuality.Bone4;output.updateWhenOffscreen=true;
                var matrix=output.worldToLocalMatrix*transform.localToWorldMatrix;var expanded=new Bounds(Vector3.up*Rig.Height*.48f,Vector3.one*Rig.Height*1.8f);var localBounds=new Bounds(matrix.MultiplyPoint3x4(expanded.center),Vector3.zero);
                for(int corner=0;corner<8;corner++)localBounds.Encapsulate(matrix.MultiplyPoint3x4(expanded.center+Vector3.Scale(expanded.extents,new Vector3((corner&1)==0?-1:1,(corner&2)==0?-1:1,(corner&4)==0?-1:1))));
                output.localBounds=localBounds;part.renderer.enabled=false;replacements.Add(part.renderer,output);
            }
            foreach(var group in GetComponentsInChildren<LODGroup>(true))
            {var lods=group.GetLODs();foreach(var lod in lods)for(int i=0;i<lod.renderers.Length;i++)if(lod.renderers[i]!=null&&replacements.TryGetValue(lod.renderers[i],out var replacement))lod.renderers[i]=replacement;group.SetLODs(lods);group.RecalculateBounds();}
            foreach(var a in GetComponentsInChildren<Animator>(true))a.enabled=false;
            CacheRest();Report="Rig automático "+Rig.Pose+" · "+total+" vértices · revisar articulaciones";Motion.Step(Rig,new HumanoidMotion{Grounded=true},.001f);
        }
        void CacheRest(){for(int i=0;i<bones.Length;i++)if(bones[i]!=null)restRotations[i]=Quaternion.Inverse(transform.rotation)*bones[i].rotation;}
        public void Tick(PoolroomsExplorer explorer,SkinPreferences settings)
        {
            if(Rig==null||explorer==null||explorer.IsPaused||!explorer.IsReady||Time.deltaTime<=0)return;
            if(!settings.animate||settings.previewBindPose){RestoreBind();return;}
            if(Time.time>=nextGround&&explorer.ViewGrounded)
            {nextGround=Time.time+.05f;leftGround=Ground(Rig.Rest[15],explorer);rightGround=Ground(Rig.Rest[19],explorer);}
            if(!explorer.ViewGrounded)leftGround=rightGround=0;
            bool swimming=explorer.world!=null&&explorer.world.TryGetWater(explorer.transform.position,out float level)&&explorer.transform.position.y<level-.75f;
            Motion.Step(Rig,new HumanoidMotion{Velocity=ToCore(explorer.ViewVelocity),Grounded=explorer.ViewGrounded,Sprint=explorer.ViewSprinting,Slide=explorer.IsSliding,Dash=explorer.IsDashing,
                Swimming=swimming,Seated=explorer.Vehicle!=null||explorer.IsAquaticRiding,Flying=explorer.IsFlying,LookPitch=explorer.ViewPitch,LeftGround=leftGround,RightGround=rightGround},Time.deltaTime,settings.intensity,settings.animationSpeed);
            ApplyPose();
        }
        float Ground(V3 foot,PoolroomsExplorer explorer)
        {
            Vector3 origin=transform.TransformPoint(ToUnity(foot))+Vector3.up*.35f;
            int count=Physics.RaycastNonAlloc(origin,Vector3.down,groundHits,.8f,~0,QueryTriggerInteraction.Ignore);if(count==groundHits.Length)return 0;
            float nearest=float.MaxValue,height=0;
            for(int i=0;i<count;i++)if(groundHits[i].collider!=null&&!groundHits[i].collider.transform.IsChildOf(explorer.transform)&&groundHits[i].normal.y>.5f&&groundHits[i].distance<nearest)
            {nearest=groundHits[i].distance;height=groundHits[i].point.y-explorer.transform.position.y;}
            return Mathf.Clamp(height,-Rig.Height*.22f,Rig.Height*.22f);
        }
        public void ApplyPose()
        {
            for(int i=0;i<bones.Length;i++)if(bones[i]!=null)
            {
                int child=HumanoidRig.Children[i],parent=HumanoidRig.Parents[i];V3 from,to;
                if(child>=0){from=Rig.Rest[child]-Rig.Rest[i];to=Motion.Joints[child]-Motion.Joints[i];}
                else if(parent>=0){from=Rig.Rest[i]-Rig.Rest[parent];to=Motion.Joints[i]-Motion.Joints[parent];}
                else{from=V3.Up;to=V3.Up;}
                var rotation=from.Length>.00001f&&to.Length>.00001f?Quaternion.FromToRotation(ToUnity(from),ToUnity(to)):Quaternion.identity;
                bones[i].SetPositionAndRotation(transform.TransformPoint(ToUnity(Motion.Joints[i])),transform.rotation*rotation*restRotations[i]);
            }
        }
        void RestoreBind(){for(int i=0;i<bones.Length;i++)if(bones[i]!=null)bones[i].SetPositionAndRotation(transform.TransformPoint(ToUnity(Rig.Rest[i])),transform.rotation*restRotations[i]);}
        void OnDestroy(){foreach(var mesh in ownedMeshes)if(mesh!=null)Destroy(mesh);}
        static V3 ToCore(Vector3 p){return new V3(p.x,p.y,p.z);}
        static Vector3 ToUnity(V3 p){return new Vector3(p.X,p.Y,p.Z);}
    }
}
