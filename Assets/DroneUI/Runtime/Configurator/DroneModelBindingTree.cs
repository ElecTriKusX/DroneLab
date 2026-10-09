using System;
using System.Linq;
using DroneLab.Configurator;
using Newtonsoft.Json.Linq;
using UnityEngine;
using UnityEngine.UIElements;

namespace DroneLab.UI
{
    internal sealed class DroneModelBindingTree : VisualElement
    {
        private readonly DroneModelViewport viewport;
        private readonly JObject bindings;
        private readonly JObject rotor;
        private string rotorId=>(string)rotor["rotorId"];
        private readonly Action changed;
        private readonly VisualElement tree,assigned;
        private readonly Label selected,status;
        private readonly Button bind;
        private Transform candidate;
        private DroneSwitch test;
        public DroneModelBindingTree(DroneModelViewport viewport,JObject bindings,JObject rotor,Action changed)
        {
            this.viewport=viewport;this.bindings=bindings;this.rotor=rotor;this.changed=changed;
            AddToClassList("drone-model-bindings");
            DroneProfileFields.Label(this,"ВИНТ В 3D-МОДЕЛИ","drone-panel-title");
            DroneProfileFields.Label(this,"Выберите лопасти в дереве. Несколько деталей можно привязать к одному ротору. Центр и ось вращения берутся из геометрии ротора.","scenario-hint");
            if(!viewport.HasImportedModel){DroneProfileFields.Label(this,"Дерево появится после импорта GLB / glTF.","scenario-hint");return;}
            assigned=new VisualElement();Add(assigned);RefreshAssigned();
            var toggle=DroneProfileFields.Button(this,"▸ Выбрать деталь из дерева",()=>{});
            var picker=new VisualElement();picker.style.display=DisplayStyle.None;Add(picker);
            toggle.clicked+=()=>{bool open=picker.style.display.value==DisplayStyle.None;picker.style.display=open?DisplayStyle.Flex:DisplayStyle.None;toggle.text=(open?"▾":"▸")+" Выбрать деталь из дерева";};
            var search=new TextField{label="Поиск узла"};picker.Add(search);
            var scroll=new ScrollView();scroll.AddToClassList("drone-model-tree");picker.Add(scroll);tree=scroll.contentContainer;
            search.RegisterValueChangedCallback(e=>Build(e.newValue));
            selected=new Label("Деталь не выбрана");selected.AddToClassList("scenario-hint");picker.Add(selected);
            bind=DroneProfileFields.Button(picker,"Привязать деталь к «"+rotorId+"»",Assign);bind.SetEnabled(false);
            status=new Label();status.AddToClassList("scenario-hint");picker.Add(status);
            test=new DroneSwitch("Проверить вращение · 60 об/мин",false);Add(test);test.ValueChanged+=value=>viewport.TestRotorSpin=value;
            Build("");
            schedule.Execute(()=>{test.SetValueWithoutNotify(viewport.TestRotorSpin);bind.text="Привязать деталь к «"+rotorId+"»";}).Every(200);
        }
        private void RefreshAssigned()
        {
            test?.SetValueWithoutNotify(false);
            assigned.Clear();var paths=DroneRotorVisuals.Paths(bindings[rotorId]).ToArray();
            if(paths.Length==0)DroneProfileFields.Label(assigned,"Лопасти ещё не привязаны","scenario-hint");
            foreach(string path in paths){
                var node=RuntimeGltfModelLoader.FindByPath(viewport.ModelRoot,path);var row=new VisualElement();row.AddToClassList("drone-model-tree-row");assigned.Add(row);
                var show=DroneProfileFields.Button(row,node!=null?node.name:"Узел отсутствует в модели",()=>viewport.SelectModelNode(node));show.style.flexGrow=1;show.tooltip=path;
                DroneProfileFields.Button(row,"×",()=>{var rest=DroneRotorVisuals.Paths(bindings[rotorId]).Where(p=>p!=path).ToArray();if(rest.Length==0)bindings.Remove(rotorId);else bindings[rotorId]=new JArray(rest);viewport.RefreshRotorBindings();changed();RefreshAssigned();});
            }
        }
        private void Build(string search)
        {
            tree.Clear();
            if(!string.IsNullOrWhiteSpace(search)){
                foreach(var node in viewport.ModelRoot.GetComponentsInChildren<Transform>(true).Where(n=>n!=viewport.ModelRoot && n.name.IndexOf(search,StringComparison.OrdinalIgnoreCase)>=0))Row(tree,node,false);
            }else for(int i=0;i<viewport.ModelRoot.childCount;i++)Row(tree,viewport.ModelRoot.GetChild(i),true);
        }
        private void Row(VisualElement host,Transform node,bool nested)
        {
            var row=new VisualElement();row.AddToClassList("drone-model-tree-row");host.Add(row);
            VisualElement children=null;
            if(nested && node.childCount>0){
                var arrow=DroneProfileFields.Button(row,"▸",()=>{});arrow.AddToClassList("drone-model-tree-arrow");
                children=new VisualElement();children.style.marginLeft=12;children.style.display=DisplayStyle.None;
                var target=children;arrow.clicked+=()=>{bool open=target.style.display.value==DisplayStyle.None;if(open && target.childCount==0)for(int i=0;i<node.childCount;i++)Row(target,node.GetChild(i),true);target.style.display=open?DisplayStyle.Flex:DisplayStyle.None;arrow.text=open?"▾":"▸";};
            }
            var button=DroneProfileFields.Button(row,node.name,()=>{candidate=node;viewport.SelectModelNode(node);selected.text=node.name;bind.SetEnabled(true);status.text="";});button.style.flexGrow=1;button.tooltip=RuntimeGltfModelLoader.PathFrom(viewport.ModelRoot,node);
            if(children!=null){host.Add(children);if(node.childCount==1){children.style.display=DisplayStyle.Flex;Row(children,node.GetChild(0),true);}}
        }
        private void Assign()
        {
            if(candidate==null)return;
            var root=viewport.ModelRoot;
            if(candidate==root || candidate.GetComponentsInChildren<Renderer>(true).Length==0 || candidate.GetComponentsInChildren<Renderer>(true).Length==root.GetComponentsInChildren<Renderer>(true).Length){status.text="Выберите отдельные лопасти, а не весь дрон.";return;}
            foreach(var property in bindings.Properties())foreach(string path in DroneRotorVisuals.Paths(property.Value)){
                var node=RuntimeGltfModelLoader.FindByPath(root,path);
                if(node!=null && (node==candidate || node.IsChildOf(candidate) || candidate.IsChildOf(node))){status.text="Эта деталь или её родитель уже привязаны к «"+property.Name+"». Сначала снимите прежнюю привязку.";return;}
            }
            var paths=DroneRotorVisuals.Paths(bindings[rotorId]).ToList();paths.Add(RuntimeGltfModelLoader.PathFrom(root,candidate));bindings[rotorId]=new JArray(paths);
            viewport.RefreshRotorBindings();changed();RefreshAssigned();status.text="Деталь привязана. Проверьте ось и центр вращения.";
        }
    }
}
