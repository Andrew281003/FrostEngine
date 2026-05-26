using System;
using System.Collections.Generic;
using System.Numerics;

namespace FrostEngine
{
    public abstract class Component
    {
        public Entity? Entity { get; set; }
        
        public virtual void Awake() {}
        public virtual void Start() {}
        public virtual void Update() {}
        public virtual void Draw() {}
    }

    public class Entity
    {
        public string Name { get; set; }
        public TransformComponent Transform { get; private set; }
        private List<Component> components = new List<Component>();

        public Entity(string name = "Entity")
        {
            Name = name;
            Transform = new TransformComponent();
            AddComponent(Transform);
        }

        public void AddComponent(Component component)
        {
            component.Entity = this;
            components.Add(component);
            component.Awake();
        }

        public T? GetComponent<T>() where T : Component
        {
            foreach (var comp in components)
            {
                if (comp is T tComp) return tComp;
            }
            return null;
        }
        
        public IEnumerable<Component> GetComponents()
        {
            return components;
        }

        public void Update()
        {
            foreach (var comp in components)
            {
                comp.Update();
            }
        }

        public void Draw()
        {
            foreach (var comp in components)
            {
                comp.Draw();
            }
        }
    }

    public class Scene
    {
        private List<Entity> entities = new List<Entity>();

        public void AddEntity(Entity entity)
        {
            entities.Add(entity);
        }

        public IEnumerable<Entity> GetEntities() => entities;

        public void Update()
        {
            foreach (var entity in entities)
            {
                entity.Update();
            }
        }

        public void Draw()
        {
            foreach (var entity in entities)
            {
                entity.Draw();
            }
        }
    }
}
