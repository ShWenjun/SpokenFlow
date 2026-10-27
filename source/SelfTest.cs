using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace SpokenFlow {
    public static class SelfTest {
        private sealed class TestTransport : System.Net.Http.HttpMessageHandler {
            public string Url,Auth,Body;
            protected override async Task<System.Net.Http.HttpResponseMessage> SendAsync(System.Net.Http.HttpRequestMessage request,CancellationToken token) {
                Url=request.RequestUri.AbsoluteUri; Auth=request.Headers.Authorization==null?null:request.Headers.Authorization.ToString(); Body=await request.Content.ReadAsStringAsync().ConfigureAwait(false);
                return new System.Net.Http.HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content=new System.Net.Http.StringContent(Chunk("Bonjour.")+End,Encoding.UTF8,"text/event-stream") };
            }
        }
        private sealed class TestClipboard : ISelectionClipboard {
            public uint Number=10;
            public bool Owner=true,Restored,CopyChanges=true,ConcurrentRead,BackupFails;
            public int Copies;
            public string Text="Selected words";
            public uint Sequence { get { return Number; } }
            public bool TargetOwnsClipboard { get { return Owner; } }
            public object Backup() { if(BackupFails) throw new InvalidOperationException("busy"); return "original"; }
            public void Copy() { Copies++; if(CopyChanges) Number++; }
            public string ReadText() { if(ConcurrentRead) Number++; return Text; }
            public void Restore(object backup) { if((string)backup!="original") throw new Exception("Backup lost"); Restored=true; }
        }
        private static string Chunk(string content) {
            return "data: {\"choices\":[{\"delta\":{\"content\":"+new System.Web.Script.Serialization.JavaScriptSerializer().Serialize(content)+"},\"finish_reason\":null}]}\n\n";
        }
        private const string End="data: {\"choices\":[{\"delta\":{},\"finish_reason\":\"stop\"}]}\n\ndata: [DONE]\n\n";
        public static int Run(string output) {
            var log=new List<string>(); int failed=0;
            Action<string,Action> test=delegate(string name,Action action) {
                File.AppendAllText(output,"RUN "+name+Environment.NewLine,Encoding.UTF8);
                try { action(); log.Add("PASS "+name); } catch(Exception ex) { failed++; log.Add("FAIL "+name+": "+ex.GetType().Name+" "+ex.Message); }
            };
            Action<bool> assert=delegate(bool condition) { if(!condition) throw new Exception("Assertion failed"); };
            test("SSE: UTF-8 text, streaming chunks, keep-alive and finish marker",delegate {
                var chunks=new List<string>();
                var result=DeepSeekClient.Parse(new StringReader(": keep-alive\n\n"+Chunk("Let's ")+Chunk("talk later.")+End),delegate(string s,long t){chunks.Add(s);},Stopwatch.StartNew(),CancellationToken.None).GetAwaiter().GetResult();
                assert(result.Text=="Let's talk later." && chunks.Count==2 && chunks[0]=="Let's " && result.FirstMs>=0);
            });
            test("SSE: reasoning is ignored",delegate {
                string data="data: {\"choices\":[{\"delta\":{\"reasoning_content\":\"hidden\"}}]}\n\n"+Chunk("Hello.")+End;
                var r=DeepSeekClient.Parse(new StringReader(data),delegate{},Stopwatch.StartNew(),CancellationToken.None).GetAwaiter().GetResult();
                assert(r.Text=="Hello.");
            });
            test("SSE: truncated stream cannot become replaceable",delegate {
                bool rejected=false; try { DeepSeekClient.Parse(new StringReader(Chunk("Partial")),delegate{},Stopwatch.StartNew(),CancellationToken.None).GetAwaiter().GetResult(); } catch(InvalidOperationException){rejected=true;} assert(rejected);
            });
            test("SSE: token limit cannot become replaceable",delegate {
                bool rejected=false; try { DeepSeekClient.Parse(new StringReader(Chunk("Partial")+"data: {\"choices\":[{\"delta\":{},\"finish_reason\":\"length\"}]}\n\ndata: [DONE]\n"),delegate{},Stopwatch.StartNew(),CancellationToken.None).GetAwaiter().GetResult(); } catch(InvalidOperationException){rejected=true;} assert(rejected);
            });
            test("SSE: empty result is rejected",delegate {
                bool rejected=false; try { DeepSeekClient.Parse(new StringReader(End),delegate{},Stopwatch.StartNew(),CancellationToken.None).GetAwaiter().GetResult(); } catch(InvalidOperationException){rejected=true;} assert(rejected);
            });
            test("SSE: cancellation invalidates result",delegate {
                bool rejected=false; var c=new CancellationTokenSource(); c.Cancel(); try { DeepSeekClient.Parse(new StringReader(Chunk("Hello")+End),delegate{},Stopwatch.StartNew(),c.Token).GetAwaiter().GetResult(); } catch(OperationCanceledException){rejected=true;} c.Dispose(); assert(rejected);
            });
            test("Replacement identity rejects changed field and changed source",delegate {
                assert(Rules.Matches("field-a","中文","field-a","中文"));
                assert(!Rules.Matches("field-a","中文","field-b","中文"));
                assert(!Rules.Matches("field-a","中文","field-a","中文更新"));
            });
            test("Chinese text detection and empty text",delegate { assert(Rules.HasChinese("Hi 你好")); assert(!Rules.HasChinese("Hello")); assert(!Rules.HasChinese("")); });
            test("Text capture accepts all seven languages and ignores numbers and symbols",delegate {
                foreach(string text in new[]{"你好","Hello","À bientôt","Grüße","Привет","안녕하세요","こんにちは"}) assert(Rules.HasLanguageText(text));
                assert(!Rules.HasLanguageText("123.45") && !Rules.HasLanguageText("  ") && !Rules.HasLanguageText("😀"));
            });
            test("Editing keeps field identity while invalidating old source",delegate {
                var a=new Snapshot { Identity="field-a",Text="你好",Foreground=(IntPtr)1 };
                var b=new Snapshot { Identity="field-a",Text="",Foreground=(IntPtr)1 };
                assert(TextCapture.SameField(a,b) && !TextCapture.Same(a,b));
                b.Foreground=(IntPtr)2; assert(!TextCapture.SameField(a,b));
                b.Foreground=(IntPtr)1; b.Identity="field-b"; assert(!TextCapture.SameField(a,b));
            });
            test("API key encrypted with current-user DPAPI",delegate { var s=new Settings(); s.Key="test-only-not-a-real-key"; assert(s.Key=="test-only-not-a-real-key"); assert(!s.ProtectedKey.Contains("test-only")); s.Key=""; assert(s.Key==""); });
            test("Browser editable combobox is eligible; suggestions and nonbrowser combos are excluded",delegate {
                assert(TextCapture.AcceptsEditor(System.Windows.Automation.ControlType.ComboBox,true));
                assert(!TextCapture.AcceptsEditor(System.Windows.Automation.ControlType.ComboBox,false));
                assert(!TextCapture.AcceptsEditor(System.Windows.Automation.ControlType.List,true));
                assert(!TextCapture.AcceptsEditor(System.Windows.Automation.ControlType.ListItem,true));
                assert(!TextCapture.AcceptsEditor(System.Windows.Automation.ControlType.Pane,true));
                assert(TextCapture.AcceptsEditor(System.Windows.Automation.ControlType.Edit,true));
                assert(TextCapture.AcceptsEditor(System.Windows.Automation.ControlType.Document,true));
                assert(TextCapture.AcceptsEditor(System.Windows.Automation.ControlType.Edit,false));
            });
            test("Current line preserves neighbouring lines, CRLF and Unicode",delegate {
                string text="keep\r\n你好😀\r\nlast"; int a,b;
                TextScopes.Line(text,8,out a,out b); assert(a==6 && text.Substring(a,b-a)=="你好😀" && text.Substring(b)=="\r\nlast");
                TextScopes.Line(text,text.Length,out a,out b); assert(text.Substring(a,b-a)=="last");
                TextScopes.Line("",0,out a,out b); assert(a==0 && b==0);
            });
            test("Office terminators excluded without trimming meaningful spaces",delegate {
                assert(TextScopes.WithoutTerminator("中文 \r\a")=="中文 ");
                assert(TextScopes.WithoutTerminator("第一行\n第二行\r")=="第一行\n第二行");
            });
            test("Undo retains scoped adapter, identity and native target",delegate {
                var original=new Snapshot { Identity="paragraph:12",Text="中文",Adapter="office:winword",Scope="当前段落",Start=12,End=14,Foreground=(IntPtr)7,NativeHandle=(IntPtr)9 };
                var changed=original.WithText("Hello");
                assert(changed.Start==12 && changed.End==17 && changed.Adapter==original.Adapter && changed.NativeHandle==original.NativeHandle && TextCapture.SameField(original,changed) && !TextCapture.Same(original,changed) && original.Text=="中文");
                changed.Identity="paragraph:18"; assert(!TextCapture.SameField(original,changed));
            });
            test("Capture worker uses STA, pumps callbacks and survives exceptions",delegate {
                var task=CaptureThread.Run(delegate { return Thread.CurrentThread.GetApartmentState(); });
                assert(task.Wait(3000) && task.Result==ApartmentState.STA);
                var bad=CaptureThread.Run<int>(delegate { throw new InvalidOperationException("test"); });
                try { bad.Wait(3000); } catch(AggregateException) { }
                assert(bad.IsFaulted);
                var next=CaptureThread.Run(delegate { return 42; }); assert(next.Wait(3000) && next.Result==42);
            });
            test("Selection geometry accepts whitespace release and reverse drag, rejects unrelated selection",delegate {
                var rectangles=new[]{new System.Windows.Rect(100,100,80,20)};
                var near=new System.Drawing.Point(110,110); var far=new System.Drawing.Point(500,110);
                assert(SelectionCapture.NearSelection(rectangles,near,far));
                assert(SelectionCapture.NearSelection(rectangles,far,near));
                assert(!SelectionCapture.NearSelection(rectangles,new System.Drawing.Point(500,400),far));
                assert(!SelectionCapture.NearSelection(new[]{System.Windows.Rect.Empty},near,far));
            });
            test("Delayed selection is retried locally and stops at first valid text",delegate {
                int reads=0,waits=0;
                string result=SelectionRetry.Read(()=>Task.FromResult(++reads==3?"Selected":null),()=>true,ms=>{waits++; return Task.FromResult(0);}).GetAwaiter().GetResult();
                assert(result=="Selected" && reads==3 && waits==2);
            });
            test("A new gesture cancels selection retry and rejects a late read",delegate {
                bool valid=true; int reads=0;
                string result=SelectionRetry.Read(()=>{reads++; return Task.FromResult<string>(null);},()=>valid,ms=>{valid=false; return Task.FromResult(0);}).GetAwaiter().GetResult();
                assert(result==null && reads==1);
                valid=true;
                result=SelectionRetry.Read(()=>{valid=false; return Task.FromResult("stale");},()=>valid,ms=>Task.FromResult(0)).GetAwaiter().GetResult();
                assert(result==null);
            });
            test("Copy fallback reads new selected text and restores eager backup",delegate {
                var clip=new TestClipboard();
                string result=SelectionCopy.Read(clip,()=>true,ms=>Task.FromResult(0)).GetAwaiter().GetResult();
                assert(result=="Selected words" && clip.Copies==1 && clip.Restored);
            });
            test("Copy fallback never translates pre-existing clipboard text",delegate {
                var clip=new TestClipboard { CopyChanges=false,Text="old secret" };
                string result=SelectionCopy.Read(clip,()=>true,ms=>Task.FromResult(0)).GetAwaiter().GetResult();
                assert(result==null && !clip.Restored);
            });
            test("Copy fallback does not consume or overwrite concurrent clipboard updates",delegate {
                var clip=new TestClipboard { ConcurrentRead=true };
                string result=SelectionCopy.Read(clip,()=>true,ms=>Task.FromResult(0)).GetAwaiter().GetResult();
                assert(result==null && !clip.Restored);
                clip=new TestClipboard { Owner=false };
                result=SelectionCopy.Read(clip,()=>true,ms=>Task.FromResult(0)).GetAwaiter().GetResult();
                assert(result==null && !clip.Restored);
            });
            test("Cancelled selection cannot start copying or translate late copied text",delegate {
                var clip=new TestClipboard(); bool valid=false;
                string result=SelectionCopy.Read(clip,()=>valid,ms=>Task.FromResult(0)).GetAwaiter().GetResult();
                assert(result==null && clip.Copies==0);
                valid=true;
                result=SelectionCopy.Read(clip,()=>valid,ms=>{valid=false; return Task.FromResult(0);}).GetAwaiter().GetResult();
                assert(result==null && clip.Restored);
            });
            test("Unpreservable clipboard aborts copying; over-limit selection is not truncated",delegate {
                var clip=new TestClipboard { BackupFails=true };
                string result=SelectionCopy.Read(clip,()=>true,ms=>Task.FromResult(0)).GetAwaiter().GetResult();
                assert(result==null && clip.Copies==0);
                clip=new TestClipboard { Text=new string('a',TextCapture.Limit+1) };
                result=SelectionCopy.Read(clip,()=>true,ms=>Task.FromResult(0)).GetAwaiter().GetResult();
                assert(result==null && clip.Restored);
            });
            test("Minimized bubble uses a compact handle and restores its full size without duplicate transitions",delegate {
                using(var bubble=new Bubble()) {
                    bubble.Size=new System.Drawing.Size(470,280);
                    int changes=0; bubble.CollapsedChanged+=delegate(bool value) { changes++; };
                    bubble.SetCollapsed(true);
                    assert(bubble.IsCollapsed && bubble.Size==new System.Drawing.Size(52,52) && changes==1);
                    bubble.SetCollapsed(true); assert(changes==1);
                    bubble.SetCollapsed(false);
                    assert(!bubble.IsCollapsed && bubble.Size==new System.Drawing.Size(470,280) && changes==2);
                    bubble.SetCollapsed(false); assert(changes==2);
                }
            });
            test("All 21 language pairs have independent symmetric cache keys",delegate {
                var keys=new HashSet<string>();
                for(int a=0;a<LanguagePair.Codes.Length;a++) for(int b=a+1;b<LanguagePair.Codes.Length;b++) {
                    var pair=new LanguagePair(LanguagePair.Codes[a],LanguagePair.Codes[b]);
                    assert(keys.Add(pair.CacheKey)); assert(pair.CacheKey==new LanguagePair(pair.B,pair.A).CacheKey);
                    assert(pair.Instruction.Contains("Identify the source language") && pair.Instruction.Contains("return the source unchanged"));
                }
                assert(keys.Count==21);
                assert(!LanguagePair.IsValid("ja","ja") && !LanguagePair.IsValid("unknown","en"));
                bool rejected=false; try { new LanguagePair("ja","ja"); } catch(ArgumentException) { rejected=true; } assert(rejected);
            });
            test("Old settings keep credentials and default to Chinese-English",delegate {
                var legacy=Settings.Parse("{\"model\":\"test-model\",\"pauseMs\":450,\"protectedKey\":\"test-encrypted-placeholder\"}");
                assert(legacy.Pair.CacheKey=="en:zh" && legacy.ProtectedKey=="test-encrypted-placeholder" && legacy.PauseMs==450 && legacy.Model=="test-model");
            });
            test("Language settings round-trip without losing credentials and reject corrupt pairs",delegate {
                var value=new Settings { LanguageA="ja",LanguageB="ko",ProtectedKey="test-encrypted-placeholder" };
                var restored=Settings.Parse(value.ToJson());
                assert(restored.LanguageA=="ja" && restored.LanguageB=="ko" && restored.ProtectedKey==value.ProtectedKey);
                restored=Settings.Parse(value.ToJson().Replace("\"ko\"","\"ja\""));
                assert(restored.LanguageA=="zh" && restored.LanguageB=="en");
            });
            test("All seven bundled UI catalogs are complete and English is the default",delegate {
                assert(L.Complete && new Settings().UiLanguage=="en");
                foreach(string code in LanguagePair.Codes) { L.SetLanguage(code); assert(!String.IsNullOrWhiteSpace(L.T("界面语言"))); }
                L.SetLanguage("invalid"); assert(L.Language=="en" && L.T("界面语言")=="Interface language");
            });
            test("UI changes translate controls but preserve user and result text",delegate {
                using(var form=new System.Windows.Forms.Form()) {
                    var label=new System.Windows.Forms.Label { Text=L.T("界面语言") };
                    var input=new System.Windows.Forms.TextBox { Text="Interface language" };
                    var resultField=new System.Windows.Forms.TextBox { Text="Interface language",ReadOnly=true };
                    form.Controls.AddRange(new System.Windows.Forms.Control[]{label,input,resultField}); L.Register(form); L.SetLanguage("ja");
                    assert(label.Text==L.T("界面语言") && input.Text=="Interface language" && resultField.Text=="Interface language");
                }
                L.SetLanguage("en");
            });
            test("Legacy settings migrate to English UI and DeepSeek without losing credentials",delegate {
                var s=Settings.Parse("{\"model\":\"test\",\"pauseMs\":350,\"protectedKey\":\"placeholder\"}");
                assert(s.UiLanguage=="en" && ApiEndpoint.IsDeepSeek(s.BaseUrl) && s.ProtectedKey=="placeholder");
                s.UiLanguage="fr"; s.BaseUrl="https://example.test/v1";
                var saved=Settings.Parse(s.ToJson()); assert(saved.UiLanguage=="fr" && saved.BaseUrl=="https://example.test/v1/chat/completions" && saved.ProtectedKey==s.ProtectedKey);
            });
            test("Endpoint normalization preserves prefixes and rejects insecure or credential-bearing URLs",delegate {
                assert(ApiEndpoint.Normalize("https://example.test/v1/")=="https://example.test/v1/chat/completions");
                assert(ApiEndpoint.Normalize("https://example.test/v1/chat/completions")=="https://example.test/v1/chat/completions");
                assert(!ApiEndpoint.NeedsKey("http://127.0.0.1:1234/v1") && ApiEndpoint.NeedsKey("https://example.test/v1"));
                foreach(string invalid in new[]{"http://example.test/v1","ftp://example.test","https://user:pass"+"@"+"example.test","https://example.test?key=value","https://example.test/#fragment",""}) {
                    bool rejected=false; try { ApiEndpoint.Normalize(invalid); } catch(InvalidOperationException) { rejected=true; } assert(rejected);
                }
                assert(!ApiEndpoint.IsDeepSeek("https://api.deepseek.com.example.test"));
            });
            test("Compatible providers receive the selected endpoint and generic streaming payload",delegate {
                var transport=new TestTransport(); using(var client=new DeepSeekClient(transport)) {
                    var result=client.TranslatePair("https://example.test/v1","test-placeholder","test-model","Hello.",new LanguagePair("en","fr"),delegate{},CancellationToken.None).GetAwaiter().GetResult();
                    assert(result.Text=="Bonjour." && transport.Url=="https://example.test/v1/chat/completions" && transport.Auth=="Bearer test-placeholder");
                    assert(transport.Body.Contains("test-model") && transport.Body.Contains("\"stream\":true") && !transport.Body.Contains("thinking") && !transport.Body.Contains("max_tokens"));
                }
                var official=DeepSeekClient.BuildPayload("https://api.deepseek.com","test","text","instruction"); assert(official.ContainsKey("thinking") && official.ContainsKey("max_tokens"));
            });
            test("Local compatible endpoints can omit authorization and retain SSE validation",delegate {
                var transport=new TestTransport(); using(var client=new DeepSeekClient(transport)) {
                    var result=client.TranslatePair("http://localhost:1234/v1","","local-model","Hello.",new LanguagePair("en","fr"),delegate{},CancellationToken.None).GetAwaiter().GetResult();
                    assert(transport.Auth==null && result.Text=="Bonjour.");
                }
            });
            log.Add("Result: "+(log.Count-failed)+" passed, "+failed+" failed. No external API calls were made.");
            File.WriteAllLines(output,log,Encoding.UTF8); return failed==0?0:1;
        }
    }
}
