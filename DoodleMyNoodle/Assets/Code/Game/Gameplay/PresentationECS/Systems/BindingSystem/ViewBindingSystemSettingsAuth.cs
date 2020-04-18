﻿using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

public class ViewBindingSystemSettingsAuth : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
{
    public ViewBindingDefinitionBank Bank;

    private List<ViewBindingDefinition> Definitions => Bank.ViewBindingDefinitions;

    public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
    {
        foreach (ViewBindingDefinition item in Definitions)
        {
            if (item)
            {
                referencedPrefabs.Add(item.GetViewGameObject());
            }
        }
    }

    public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
    {
        var buffer = dstManager.AddBuffer<Settings_ViewBindingSystem_Binding>(entity);
        buffer.Capacity = Definitions.Count;

        foreach (ViewBindingDefinition item in Definitions)
        {
            if (item)
            {
                buffer.Add(new Settings_ViewBindingSystem_Binding()
                {
                    SimAssetId = item.GetSimAssetId(),
                    PresentationEntity = conversionSystem.GetPrimaryEntity(item.GetViewGameObject())
                });
            }
        }
    }

    //BlobAssetReference<ViewBindingSystemSettings> CreateBlobAsset(GameObjectConversionSystem conversionSystem)
    //{
    //    BlobBuilder blobBuilder = new BlobBuilder(Allocator.Temp);

    //    ref ViewBindingSystemSettings root = ref blobBuilder.ConstructRoot<ViewBindingSystemSettings>();

    //    var ids = blobBuilder.Allocate(ref root.BlueprintIds, BlueprintDefinitions.Count);
    //    var viewEntities = blobBuilder.Allocate(ref root.BlueprintPresentationEntities, BlueprintDefinitions.Count);

    //    for (int i = 0; i < BlueprintDefinitions.Count; i++)
    //    {
    //        ids[i] = BlueprintDefinitions[i].GetBlueprintId().Value;
    //        viewEntities[i] = conversionSystem.GetPrimaryEntity(BlueprintDefinitions[i].GetViewGameObject());
    //    }

    //    BlobAssetReference<ViewBindingSystemSettings> blobRef = blobBuilder.CreateBlobAssetReference<ViewBindingSystemSettings>(Allocator.Persistent);

    //    blobBuilder.Dispose();

    //    return blobRef;
    //}
}
