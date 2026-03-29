using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLBasic_net.Datas;
using ColorPicker.Models;
using SQLBasic_net.Services;
using ICSharpCode.AvalonEdit;
using ICSharpCode.AvalonEdit.Highlighting;
using ICSharpCode.AvalonEdit.Highlighting.Xshd;
using System.Xml;
using ICSharpCode.AvalonEdit.Document;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TrayNotify;

namespace SQLBasic_net.ViewModels;

public partial class SetSyntaxViewModel : ObservableObject
{
    #region LabelName
    private readonly string[] _labelName = {
            "背景","数値","コメント","句読点","文字列","ラベル","キーワード","関数","カラム","コメント内(TODO,FIXME","コメント内(HACK,UNDONE)"
        };
    #endregion

    [ObservableProperty]
    private ColorState _selectColorPicker = new ColorState()
    {
        A = 1,
        RGB_B = 1,
    };
    [ObservableProperty]
    private TextDocument _sampleText = new TextDocument();

    [ObservableProperty]
    private Brush _backGround = Brushes.Black;

    [ObservableProperty]
    private IHighlightingDefinition? _syntaxSet;

    [ObservableProperty]
    private List<SyntaxItem> _syntaxList = new List<SyntaxItem>();

    [ObservableProperty]
    private SyntaxItem? _selectSyntaxItem = new SyntaxItem();

    private readonly ICoreService _coreService;

    public Action? WindowClose;

    public SetSyntaxViewModel(ICoreService coreService)
    {
        _coreService = coreService;

        #region エディタの設定
        SampleText.Text = @"/* aaa */
select
 a.aaa as ""Lb1"" -- label1
 , avg(a.bbb) as ""Lb2""
from
 atable a
where
 a.bbb = 3.5 and a.ccc = 'aaa'
order by a.aaa
";
        #endregion

        for (int i = 0; i < 11; i++)
        {
            SyntaxList.Add(new SyntaxItem() { No = i, Name = _labelName[i], Color = _coreService.GetSyntaxColor(i) });
        }

        ChangeColor();
    }
    [RelayCommand]
    private void OK()
    {
        foreach (var item in SyntaxList)
        {
            _coreService.SetSyntaxColor(item.No, item.Color);
        }
        WindowClose?.Invoke();
    }

    [RelayCommand]
    private void Cancel()
    {
        WindowClose?.Invoke();
    }
    public void ChangeColor()
    {
        BackGround = SyntaxList[0].Color;

        List<string> c = new List<string>();

        foreach (var item in SyntaxList)
        {
            if (item.No == 0)
                continue;
            c.Add(_coreService.GetStringColorCode(item.Color));
        }

        var xml = _coreService.GetSyntaxXml(c.ToArray());
        using (var stringReader = new System.IO.StringReader(xml))
        {
            using (var reader = XmlReader.Create(stringReader))
            {
                var xshd = HighlightingLoader.LoadXshd(reader);
                SyntaxSet = HighlightingLoader.Load(xshd, HighlightingManager.Instance);
            }
        }
    }
}
