using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Unity.LiveCapture.CompanionApp
{
    interface ITakeManager
    {
        void SelectTake(ISlate slate, SerializableGuid guid);
        void SetTakeData(TakeDescriptor descriptor);
        void DeleteTake(SerializableGuid guid);
        void SetIterationBase(ISlate slate, SerializableGuid guid);
        void ClearIterationBase(ISlate slate);
    }

    class TakeManager : ITakeManager
    {
        public void SelectTake(ISlate slate, SerializableGuid guid)
        {
            if (slate == null)
            {
                throw new ArgumentNullException(nameof(slate));
            }

#if UNITY_EDITOR
            var take = AssetDatabaseUtility.LoadAssetWithGuid<Take>(guid.ToString());

            slate.Take = take;
#endif
        }

        public void SetTakeData(TakeDescriptor descriptor)
        {
#if UNITY_EDITOR
            var take = AssetDatabaseUtility.LoadAssetWithGuid<Take>(descriptor.Guid.ToString());

            if (take != null)
            {
                var assetName = TakeBuilder.GetAssetName(
                    descriptor.SceneNumber,
                    descriptor.ShotName,
                    descriptor.TakeNumber);

                take.name = assetName;
                take.SceneNumber = descriptor.SceneNumber;
                take.ShotName = descriptor.ShotName;
                take.TakeNumber = descriptor.TakeNumber;
                take.Description = descriptor.Description;
                take.Rating = descriptor.Rating;
                take.FrameRate = descriptor.FrameRate;

                EditorUtility.SetDirty(take);

                AssetDatabase.SaveAssets();
            }
#endif
        }

        public void DeleteTake(SerializableGuid guid)
        {
#if UNITY_EDITOR
            var path = AssetDatabase.GUIDToAssetPath(guid.ToString());
            var take = AssetDatabase.LoadAssetAtPath<Take>(path);

            if (take != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
#endif
        }

        public void SetIterationBase(ISlate slate, SerializableGuid guid)
        {
            if (slate == null)
            {
                throw new ArgumentNullException(nameof(slate));
            }

#if UNITY_EDITOR
            var take = AssetDatabaseUtility.LoadAssetWithGuid<Take>(guid.ToString());

            if (take != null)
            {
                slate.IterationBase = take;
            }
#endif
        }

        public void ClearIterationBase(ISlate slate)
        {
            if (slate == null)
            {
                throw new ArgumentNullException(nameof(slate));
            }

            slate.IterationBase = null;
        }
    }
}
