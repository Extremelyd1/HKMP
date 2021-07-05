using System.Collections.Generic;
using UnityEngine;

namespace Hkmp.Util {
    public static class CopyUtil {
        /**
         * Make a copy of a tk2dSpriteAnimation object, this will preserve internal references in the object.
         * The targetObject parameter given is used to initialize Unity related components in.
         */
        public static tk2dSpriteAnimation SmartCopySpriteAnimation(tk2dSpriteAnimation original,
            GameObject targetObject) {
            // Keep track of internal references between object in the original objects.
            // Every time we make a copy of an object we add the original object as a key and
            // the new object as a value. Then when we encounter an object in the original that we
            // already made a copy of, we can simply retrieve the value from this dictionary.
            // That way we preserve references between objects from the original.
            var objectDict = new Dictionary<object, object>();

            var newSpriteAnimation = targetObject.AddComponent<tk2dSpriteAnimation>();

            var originalClips = original.clips;

            // The only member variable in the sprite animation class in the array with clips
            newSpriteAnimation.clips = new tk2dSpriteAnimationClip[originalClips.Length];

            for (var i = 0; i < originalClips.Length; i++) {
                var originalClip = originalClips[i];

                if (objectDict.ContainsKey(originalClip)) {
                    newSpriteAnimation.clips[i] = (tk2dSpriteAnimationClip) objectDict[originalClip];
                } else {
                    var newSpriteAnimationClip = SmartCopySpriteAnimationClip(originalClip, targetObject, objectDict);
                    newSpriteAnimation.clips[i] = newSpriteAnimationClip;

                    objectDict[originalClip] = newSpriteAnimationClip;
                }
            }

            return newSpriteAnimation;
        }

        private static tk2dSpriteAnimationClip SmartCopySpriteAnimationClip(
            tk2dSpriteAnimationClip original,
            GameObject targetObject,
            Dictionary<object, object> objectDict
        ) {
            // Create a new instance and copy simple values
            var newAnimationClip = new tk2dSpriteAnimationClip {
                name = original.name,
                fps = original.fps,
                loopStart = original.loopStart,
                wrapMode = original.wrapMode
            };

            // Now we need to deep copy the frame array
            var originalFrames = original.frames;

            if (objectDict.ContainsKey(originalFrames)) {
                newAnimationClip.frames = (tk2dSpriteAnimationFrame[]) objectDict[originalFrames];
                return newAnimationClip;
            }

            newAnimationClip.frames = new tk2dSpriteAnimationFrame[originalFrames.Length];

            for (var i = 0; i < originalFrames.Length; i++) {
                var originalFrame = originalFrames[i];

                if (objectDict.ContainsKey(originalFrame)) {
                    newAnimationClip.frames[i] = (tk2dSpriteAnimationFrame) objectDict[originalFrame];
                } else {
                    var newAnimationFrame = SmartCopySpriteAnimationFrame(originalFrame, targetObject, objectDict);
                    newAnimationClip.frames[i] = newAnimationFrame;

                    objectDict[originalFrame] = newAnimationFrame;
                }
            }

            return newAnimationClip;
        }

        private static tk2dSpriteAnimationFrame SmartCopySpriteAnimationFrame(
            tk2dSpriteAnimationFrame original,
            GameObject targetObject,
            Dictionary<object, object> objectDict
        ) {
            // Create a new instance and copy simply values
            var newAnimationFrame = new tk2dSpriteAnimationFrame {
                spriteId = original.spriteId,
                triggerEvent = original.triggerEvent,
                eventInfo = original.eventInfo,
                eventInt = original.eventInt,
                eventFloat = original.eventFloat
            };

            // Now we need to copy the sprite collection
            if (objectDict.ContainsKey(original.spriteCollection)) {
                newAnimationFrame.spriteCollection = (tk2dSpriteCollectionData) objectDict[original.spriteCollection];
            } else {
                var newSpriteCollectionData =
                    SmartCopySpriteCollectionData(original.spriteCollection, targetObject, objectDict);
                newAnimationFrame.spriteCollection = newSpriteCollectionData;

                objectDict[original.spriteCollection] = newSpriteCollectionData;
            }

            return newAnimationFrame;
        }

        private static tk2dSpriteCollectionData SmartCopySpriteCollectionData(
            tk2dSpriteCollectionData original,
            GameObject targetObject,
            Dictionary<object, object> objectDict
        ) {
            var newSpriteCollectionData = targetObject.AddComponent<tk2dSpriteCollectionData>();

            newSpriteCollectionData.version = original.version;
            newSpriteCollectionData.materialIdsValid = original.materialIdsValid;
            newSpriteCollectionData.needMaterialInstance = original.needMaterialInstance;
            newSpriteCollectionData.premultipliedAlpha = original.premultipliedAlpha;
            newSpriteCollectionData.material = SmartCopyMaterial(original.material, objectDict);

            newSpriteCollectionData.materials = SmartCopyMaterialArray(original.materials, objectDict);
            newSpriteCollectionData.materialInsts = SmartCopyMaterialArray(original.materials, objectDict);

            // This is not a deep copy, but hopefully these array won't dynamically change
            newSpriteCollectionData.textureInsts = SmartCopyArray(original.textureInsts, objectDict);
            newSpriteCollectionData.textures = SmartCopyArray(original.textures, objectDict);
            newSpriteCollectionData.pngTextures = SmartCopyArray(original.pngTextures, objectDict);
            newSpriteCollectionData.materialPngTextureId = SmartCopyArray(original.materialPngTextureId, objectDict);

            newSpriteCollectionData.textureFilterMode = original.textureFilterMode;
            newSpriteCollectionData.textureMipMaps = original.textureMipMaps;
            newSpriteCollectionData.allowMultipleAtlases = original.allowMultipleAtlases;
            newSpriteCollectionData.spriteCollectionGUID = original.spriteCollectionGUID;
            newSpriteCollectionData.spriteCollectionName = original.spriteCollectionName;
            newSpriteCollectionData.assetName = original.assetName;
            newSpriteCollectionData.loadable = original.loadable;
            newSpriteCollectionData.invOrthoSize = original.invOrthoSize;
            newSpriteCollectionData.halfTargetHeight = original.halfTargetHeight;
            newSpriteCollectionData.buildKey = original.buildKey;
            newSpriteCollectionData.dataGuid = original.dataGuid;
            newSpriteCollectionData.managedSpriteCollection = original.managedSpriteCollection;
            newSpriteCollectionData.hasPlatformData = original.hasPlatformData;

            newSpriteCollectionData.spriteCollectionPlatforms =
                SmartCopyArray(original.spriteCollectionPlatforms, objectDict);
            newSpriteCollectionData.spriteCollectionPlatformGUIDs =
                SmartCopyArray(original.spriteCollectionPlatformGUIDs, objectDict);

            newSpriteCollectionData.Transient = original.Transient;

            // Now we smart copy the sprite definitions
            var originalDefinitions = original.spriteDefinitions;

            if (objectDict.ContainsKey(originalDefinitions)) {
                newSpriteCollectionData.spriteDefinitions = (tk2dSpriteDefinition[]) objectDict[originalDefinitions];
            } else {
                newSpriteCollectionData.spriteDefinitions = new tk2dSpriteDefinition[originalDefinitions.Length];
                for (var i = 0; i < originalDefinitions.Length; i++) {
                    var originalDefinition = originalDefinitions[i];

                    if (objectDict.ContainsKey(originalDefinition)) {
                        newSpriteCollectionData.spriteDefinitions[i] =
                            (tk2dSpriteDefinition) objectDict[originalDefinition];
                    } else {
                        var newSpriteDefinition = SmartCopySpriteDefinition(originalDefinition, objectDict);
                        newSpriteCollectionData.spriteDefinitions[i] = newSpriteDefinition;

                        objectDict[originalDefinition] = newSpriteDefinition;
                    }
                }
            }

            // Initialize sprite lookup dictionary after the sprite definitions are set
            newSpriteCollectionData.InitDictionary();

            return newSpriteCollectionData;
        }

        private static tk2dSpriteDefinition SmartCopySpriteDefinition(
            tk2dSpriteDefinition original,
            Dictionary<object, object> objectDict
        ) {
            var newSpriteDefinition = new tk2dSpriteDefinition {
                name = original.name,
                boundsData = SmartCopyArray(original.boundsData, objectDict),
                untrimmedBoundsData = SmartCopyArray(original.untrimmedBoundsData, objectDict),
                texelSize = original.texelSize,
                positions = SmartCopyArray(original.positions, objectDict),
                normals = SmartCopyArray(original.normals, objectDict),
                tangents = SmartCopyArray(original.tangents, objectDict),
                uvs = SmartCopyArray(original.uvs, objectDict),
                normalizedUvs = SmartCopyArray(original.normalizedUvs, objectDict),
                indices = SmartCopyArray(original.indices, objectDict),
                material = SmartCopyMaterial(original.material, objectDict),
                materialInst = SmartCopyMaterial(original.materialInst, objectDict),
                materialId = original.materialId,
                sourceTextureGUID = original.sourceTextureGUID,
                extractRegion = original.extractRegion,
                regionX = original.regionX,
                regionY = original.regionY,
                regionW = original.regionW,
                regionH = original.regionH,
                flipped = original.flipped,
                complexGeometry = original.complexGeometry,
                physicsEngine = original.physicsEngine,
                colliderType = original.colliderType,
                // We do not deep copy this as we probably want to preserve these colliders
                customColliders = original.customColliders,
                colliderVertices = original.colliderVertices,
                colliderIndicesFwd = original.colliderIndicesFwd,
                colliderIndicesBack = original.colliderIndicesBack,
                colliderConvex = original.colliderConvex,
                colliderSmoothSphereCollisions = original.colliderSmoothSphereCollisions,
                polygonCollider2D = original.polygonCollider2D,
                edgeCollider2D = original.edgeCollider2D,
                attachPoints = original.attachPoints
            };

            return newSpriteDefinition;
        }

        private static Material SmartCopyMaterial(Material original, Dictionary<object, object> objectDict) {
            // First check whether the given material is null, because then we can return null as well
            if (original == null) {
                return null;
            }

            if (objectDict.ContainsKey(original)) {
                return (Material) objectDict[original];
            }

            var newMaterial = new Material(original);
            objectDict[original] = newMaterial;

            return newMaterial;
        }

        private static Material[] SmartCopyMaterialArray(Material[] original, Dictionary<object, object> objectDict) {
            if (objectDict.ContainsKey(original)) {
                return (Material[]) objectDict[original];
            }

            var newMaterials = new Material[original.Length];
            for (var i = 0; i < original.Length; i++) {
                newMaterials[i] = SmartCopyMaterial(original[i], objectDict);
            }

            objectDict[original] = newMaterials;

            return newMaterials;
        }

        private static T[] SmartCopyArray<T>(T[] original, Dictionary<object, object> objectDict) {
            if (objectDict.ContainsKey(original)) {
                return (T[]) objectDict[original];
            }

            var newArray = new T[original.Length];
            for (var i = 0; i < original.Length; i++) {
                newArray[i] = original[i];
            }

            objectDict[original] = newArray;

            return newArray;
        }
    }
}