using ICSharpCode.AvalonEdit.CodeCompletion;
using ICSharpCode.AvalonEdit.Document;
using ICSharpCode.AvalonEdit.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace SQLBasic_net.Services;

public class MyCompletionData: ICompletionData
{
    public MyCompletionData(string text) => Text = text;
    public ImageSource? Image => null;
    public string Text { get; }
    public object Content => Text;
    public object Description => $"Column: {Text}";
    public double Priority => 0;

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
        => textArea.Document.Replace(completionSegment, Text);
}
