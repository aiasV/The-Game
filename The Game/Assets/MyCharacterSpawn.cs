using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DungeonArchitect;
using MoreMountains.TopDownEngine;

public class MyCharacterSpawn : MonoBehaviour
{
public Dungeon dungeon; // Assign this in the inspector

    // private void Start()
    // {
    //     if (dungeon != null)
    //     {
    //         // Find a marker with the name "SpawnPoint"
    //         var markers = dungeon.gameObject.GetComponentsInChildren<Marker>();
    //         foreach (var marker in markers)
    //         {
    //             if (marker.name == "SpawnPoint")
    //             {
    //                 // Set the spawn point position to the marker's position
    //                 CharacterSpawn spawn = GetComponent<CharacterSpawn>();
    //                 if (spawn != null)
    //                 {
    //                     spawn.SpawnPosition = marker.transform.position;
    //                 }
                    
    //                 // Stop the loop once the spawn point is found
    //                 break;
    //             }
    //         }
    //     }
    // }
}
