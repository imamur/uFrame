using System.CodeDom;
using System.Linq;
using Invert.Core.GraphDesigner;
using Invert.uFrame.Editor;
using uFrame.Graphs;

[TemplateClass("SceneManagers", uFrameFormats.SCENE_MANAGER_FORMAT, MemberGeneratorLocation.Both)]
public class SceneManagerTemplate : SceneManager, IClassTemplate<SceneManagerNode>
{
    public void TemplateSetup()
    {
        Ctx.TryAddNamespace("UniRx");
        foreach (var transition in Ctx.Data.Transitions)
        {
            var to = transition.OutputTo<SceneManagerNode>();
            if (to == null) continue;
            Ctx._("public {0} {1}Transition = new {0}();",to.Name.AsSceneManagerSettings(), transition.Name.AsField());
        }
        if (Ctx.IsDesignerFile)
        Ctx._("public {0} {1} = new {0}();", Ctx.Data.Name.AsSceneManagerSettings(), Ctx.Data.Name.AsSceneManagerSettings().AsField());

        Ctx.AddIterator("InstanceProperty", node=>node.ImportedItems);
        Ctx.AddIterator("ControllerProperty", node => node.IncludedElements);
        Ctx.AddIterator("GetTransitionScenes", node => node.Transitions);
        Ctx.AddIterator("TransitionMethod", node => node.Transitions);
        Ctx.AddIterator("TransitionComplete", node => node.Transitions);
    }

    public TemplateContext<SceneManagerNode> Ctx { get; set; }

    
    //[Inject("LocalPlayer")]
    [TemplateProperty("{0}",AutoFillType.NameOnly)]
    public virtual ViewModel InstanceProperty {
        get
        {
            var instance = Ctx.ItemAs<RegisteredInstanceReference>();
            Ctx.SetType(instance.SourceItem.Name.AsViewModel());

            Ctx.AddAttribute(typeof (InjectAttribute))
                .Arguments.Add(new CodeAttributeArgument(new CodePrimitiveExpression(instance.Name)));

            Ctx._if("this.{0} == null",instance.Name.AsField())
                .TrueStatements._("this.{0} = CreateInstanceViewModel<{1}>({2}, \"{0}\")",instance.Name.AsField(), instance.SourceItem.Name.AsViewModel(), instance.SourceItem.Name.AsController());

            Ctx.CurrentDecleration._private_(Ctx.CurrentProperty.Type, instance.Name.AsField());
            Ctx._("return {0}",instance.Name.AsField());

            //if ((this._LocalPlayer == null)) {
            //    this._LocalPlayer = CreateInstanceViewModel<FPSPlayerViewModel>(FPSPlayerController, "LocalPlayer");
            //}
            //return this._LocalPlayer;
            return null;
        }
        set {
            //_LocalPlayer = value;
        }
    }
    
    //[Inject()]
    [TemplateProperty(uFrameFormats.CONTROLLER_FORMAT, AutoFillType.NameOnly)]
    public virtual Controller ControllerProperty {
        get
        {
            Ctx.SetType(Ctx.Item.Name.AsController());
            Ctx.AddAttribute(typeof (InjectAttribute));
            Ctx.CurrentDecleration._private_(Ctx.CurrentProperty.Type, Ctx.Item.Name.AsController().AsField());
            Ctx.LazyGet(Ctx.Item.Name.AsController().AsField(), "new {0}() {{ Container = Container }}", Ctx.Item.Name.AsController());
            return null;
        }
        set {
            Ctx._("{0} = value", Ctx.Item.Name.AsController().AsField());
        }
    }
   
    // <summary>
    // This method is the first method to be invoked when the scene first loads. Anything registered here with 'Container' will effectively 
    // be injected on controllers, and instances defined on a subsystem.And example of this would be Container.RegisterInstance<IDataRepository>(new CodeRepository()). Then any property with 
    // the 'Inject' attribute on any controller or view-model will automatically be set by uFrame. 
    // </summary>
    [TemplateMethod()]
    public override void Setup() {
        base.Setup();
        foreach (var item in Ctx.Data.ImportedItems)
        {
            Ctx._("Container.RegisterViewModel<{0}>({1}, \"{1}\")",item.SourceItem.Name.AsViewModel(),item.Name,item.Name);
        }
        foreach (var item in Ctx.Data.IncludedElements)
        {
            Ctx._("Container.RegisterController<{0}>({0})", item.Name.AsController());
        }
        Ctx._("Container.InjectAll()");
        foreach (var item in Ctx.Data.ImportedItems)
        {
            Ctx._("{0}.Initialize({1})", item.SourceItem.Name.AsController(), item.Name);
        }

    }


    #region Transitions

    [TemplateMethod("Get{0}Scenes", MemberGeneratorLocation.DesignerFile, false, AutoFill = AutoFillType.NameOnly)]
    public virtual System.Collections.Generic.IEnumerable<string> GetTransitionScenes() {
        Ctx._("return {0}Transition._Scenes", Ctx.Item.Name.AsField());
        //return this._QuitGameTransition._Scenes;
        return null;
    }

    [TemplateMethod(AutoFill = AutoFillType.NameOnly)]
    public virtual void TransitionMethod()
    {

        var transition = Ctx.ItemAs<SceneManagerTransitionReference>();
        var transitionCommand = transition.SourceItem as CommandChildItem;
        var transitionOutput = transition.OutputTo<SceneManagerNode>();
        if (transitionOutput != null)
        {
            Ctx._("GameManager.TransitionLevel<{0}>((container) =>{{container.{1} = _{2}Transition; {2}TransitionComplete(container); }}, this.Get{2}Scenes().ToArray())",
                transitionOutput.Name.AsSceneManager(), transitionOutput.Name.AsSceneManagerSettings().AsField(),transition.Name);
        }
        
        //GameManager.TransitionLevel<FPSMainMenuManager>((container) =>{container._FPSMainMenuManagerSettings = _QuitGameTransition; QuitGameTransitionComplete(container); }, this.GetQuitGameScenes().ToArray());
    }

    [TemplateMethod("{0}TransitionComplete", MemberGeneratorLocation.Both,true)]
    public virtual void TransitionComplete(object sceneManager)
    {
        //if (!Ctx.IsDesignerFile) return;
        var transition = Ctx.ItemAs<SceneManagerTransitionReference>();
        var transitionOutput = transition.OutputTo<SceneManagerNode>();
        if (transition == null) return;
        Ctx.CurrentMethod.Parameters[0].Type = new CodeTypeReference(transitionOutput.Name.AsSceneManager());
    }
    
    public override void Initialize()
    {
        base.Initialize();

        //foreach (var item in Ctx.Data.Trnasitions)
        //{

        //    Ctx._("{0}.{1}.Subscribe(_=> {2}()).DisposeWith(this.gameObject)",item);
        //}
        // FPSGame.MainMenu.Subscribe(_=> MainMenu()).DisposeWith(this.gameObject);
        // FPSGame.QuitGame.Subscribe(_=> QuitGame()).DisposeWith(this.gameObject);
    }

    #endregion


 
}