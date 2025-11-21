using Godot;

namespace Core.ECS;

/// <summary>
/// Bootstrap node responsible for initializing and registering ECS entity factories.
/// </summary>
/// <remarks>
/// This node should be added to the main scene so that all entity factories are
/// registered automatically at runtime. It uses Godot's scene tree to attach
/// instantiated entity roots for ECS entities.
/// </remarks>
[GlobalClass]
public partial class EntityBootstrap : Node
{
    /// <inheritdoc/>
    public override void _EnterTree() => RegisterFactories();

    /// <summary>
    /// Registers factory handlers for each type of <see cref="EntitySpawnData"/>.
    /// </summary>
    /// <remarks>
    /// Each factory function is responsible for creating the corresponding <see cref="EntityRoot"/>
    /// and adding it to the Godot scene tree if necessary.
    /// </remarks>
    private static void RegisterFactories()
    {
        RegisterScene<EntitySpawnData, EntityRoot>("res://scenes/EntityRoot.tscn");
    }

    /// <summary>
    /// Registers a standard ECS entity factory that instantiates a scene-based entity root from a PackedScene.
    /// </summary>
    /// <typeparam name="TData">
    /// The concrete type of <see cref="EntitySpawnData"/> used to spawn this entity.
    /// </typeparam>
    /// <typeparam name="TRoot">
    /// The type of <see cref="EntityRoot"/> to instantiate from the scene.
    /// </typeparam>
    /// <param name="scenePath">
    /// The path/UID to the PackedScene resource (e.g., "res://scenes/PlayerRoot.tscn").
    /// </param>
    public static void RegisterScene<TData, TRoot>(string scenePath)
        where TData : EntitySpawnData
        where TRoot : EntityRoot
    {
        EntityFactory.Register<TData>(data =>
        {
            var packedScene = GD.Load<PackedScene>(scenePath);
            var root = packedScene.Instantiate<TRoot>();
            return root;
        });
    }
}
