﻿module SlideDefinition
open CodeDefinitionImperative
open CodeDefinitionLambda
open Coroutine
open CommonLatex
open Runtime
open TypeChecker
open Interpreter

type SlideElement = 
  | Section of string 
  | Advanced of SlideElement
  | SubSection of string 
  | Pause
  | Question of string
  | InlineCode of string
  | Text of string
  | Block of SlideElement
  | Items of List<SlideElement>
  | PythonCodeBlock of TextSize * Code
  | LambdaCodeBlock of TextSize * Term
  | CSharpCodeBlock of TextSize * Code
  | Unrepeated of SlideElement
  | Tiny
  | Small
  | Normal
  | Large
  | TypingRules of List<TypingRule>
  | VerticalStack of List<SlideElement>
  | PythonStateTrace of TextSize * Code * RuntimeState<Code>
  | CSharpStateTrace of TextSize * Code * RuntimeState<Code>
  | CSharpTypeTrace of TextSize * Code * TypeCheckingState<Code>
  | LambdaStateTrace of textSize:TextSize * term:Term * maxSteps:Option<int> * showArithmetics:bool * showControlFlow:bool * showLet:bool * showPairs:bool * showUnions:bool
  with
    member this.ToStringAsElement() = 
      match this with
      | Pause -> @"\pause", []
      | Question q -> 
          sprintf @"%s\textit{%s}%s" beginExampleBlock q endExampleBlock, []
      | InlineCode c -> sprintf @"\texttt{%s}" c, []
      | Text t -> t, []
      | Tiny -> sprintf "\\tiny\n", []
      | Small -> sprintf "\\small\n", []
      | Normal -> sprintf "\\normal\n", []
      | Large -> sprintf "\\large\n", []
      | Block t ->
        let ts,k = t.ToStringAsElement()
        sprintf @"%s%s%s" beginExampleBlock ts endExampleBlock, k
      | Items items ->
        let items = items |> List.map (function | Pause -> @"\pause",[] | item -> let i,k = item.ToStringAsElement() in @"\item " + i + "\n", k)
        let k = items |> List.map snd |> List.fold (@) []
        let items = items |> List.map fst
        sprintf @"%s%s%s" beginItemize (items |> Seq.fold (+) "" ) endItemize, []
      | PythonCodeBlock (ts,c) ->
          let textSize = ts.ToString()
          sprintf @"\lstset{basicstyle=\ttfamily%s}%s%s%s" textSize (beginCode "Python") (c.AsPython "") endCode, []
      | LambdaCodeBlock(ts, c) ->
          let textSize = ts.ToString()
          sprintf @"\lstset{basicstyle=\ttfamily%s}\lstset{numbers=none}%s%s%s" textSize (beginCode "Python") (c.ToLambda) endCode, []
      | CSharpCodeBlock (ts,c) ->
          let textSize = ts.ToString()
          let javaVersion = "\n\n" + sprintf @"%s %sWhich in Java then becomes:%s \lstset{basicstyle=\ttfamily%s}%s%s%s%s" beginFrame beginExampleBlock endExampleBlock textSize (beginCode "Java") (c.AsJava "") endCode endFrame
          sprintf @"\lstset{basicstyle=\ttfamily%s}%s%s%s" textSize (beginCode "[Sharp]C") (c.AsCSharp "") endCode, 
            [
              javaVersion
            ]
      | TypingRules tr ->
          let trs = tr |> List.map (fun t -> t.ToString())
          (List.fold (+) "" trs), []
      | VerticalStack items ->
          let items = items |> List.map (fun item -> let i,k = item.ToStringAsElement() in @"\item " + i + "\n", k)
          let k = items |> List.map snd |> List.fold (@) []
          let items = items |> List.map fst
          let allItems = items |> List.map (fun i -> i + " \n") |> List.fold (+) ""
          allItems,k
      | _ -> "", []
    override this.ToString() =
      match this with
      | Section t ->
        sprintf @"\SlideSection{%s}%s" t "\n"
      | SubSection t ->
        sprintf @"\SlideSubSection{%s}%s" t "\n"
      | Advanced se ->
        //TODO: \footnote{Warning: this material is to be considered advanced!}
        se.ToString()
      | Block t ->
        let content,rest = t.ToStringAsElement()
        sprintf @"%s%s%s%s%s" beginFrame beginBlock content endBlock endFrame
      | Pause -> @"\pause"
      | Question q ->
        sprintf @"%s%s\textit{%s}%s%s" beginFrame beginExampleBlock q endExampleBlock endFrame
      | InlineCode c ->
          sprintf @"%s\texttt{%s}%s" (beginCode "Python") c endCode
      | Text t -> t
      | Items items ->
        let items = items |> List.map (function | Pause -> @"\pause",[] | item -> let i,k = item.ToStringAsElement() in @"\item " + i + "\n", k)
        let k = items |> List.map snd |> List.fold (@) []
        let items = items |> List.map fst
        sprintf @"%s%s%s%s%s" beginFrame beginItemize (items |> Seq.fold (+) "") endItemize endFrame
      | PythonCodeBlock (ts,c) ->
          let textSize = ts.ToString()
          sprintf @"%s\lstset{basicstyle=\ttfamily%s}%s%s%s%s" beginFrame textSize (beginCode "Python") (c.AsPython "") endCode endFrame
      | LambdaCodeBlock (ts, c) ->
          let textSize = ts.ToString()
          sprintf @"%s\lstset{basicstyle=\ttfamily%s}\lstset{numbers=none}%s%s%s%s" beginFrame textSize (beginCode "ML") (c.ToLambda) endCode endFrame
      | CSharpCodeBlock (ts,c) ->
          let textSize = ts.ToString()
          (sprintf @"%s\lstset{basicstyle=\ttfamily%s}%s%s%s%s" beginFrame textSize (beginCode "[Sharp]C")  (c.AsCSharp "") endCode endFrame) + "\n\n" +
            (sprintf @"%s%sWhich in Java then becomes:%s \lstset{basicstyle=\ttfamily%s}%s%s%s%s" beginFrame beginExampleBlock endExampleBlock textSize (beginCode "Java")  (c.AsJava "") endCode endFrame)
      | TypingRules tr ->
          let trs = tr |> List.map (fun t -> t.ToString())
          sprintf @"%s%s%s" beginFrame (List.fold (+) "" trs) endFrame
      | VerticalStack items ->
          let items = items |> List.map (fun item -> item.ToStringAsElement())
          let k = items |> List.map snd |> List.fold (@) [] |> List.map (fun x -> x + "\n") |> List.fold (+) ""
          let items = items |> List.map fst
          let allItems = items |> List.map (fun i -> i + " \n") |> List.fold (+) ""
          (sprintf @"%s%s%s" beginFrame allItems endFrame) + k
      | PythonStateTrace(ts,p,st) ->
        let textSize = ts.ToString()
        let stackTraces = st :: runToEnd (runPython p) st
        let ps = (p.AsPython "").TrimEnd([|'\n'|])
        let stackTraceTables = 
          [ for st in stackTraces do 
            let stack,heap,output,input = st.AsSlideContent Dots (function Code.Hidden _ -> true | _ -> false) (fun c -> c.AsPython)
            let input = if input = "" then "" else "Input: " + input + @"\\"
            let output = if output = "" then "" else "Output: " + output + @"\\"
            let heap = if heap = "" then "" else "Heap: " + heap + @"\\"
            let slide = sprintf @"%s\lstset{basicstyle=\ttfamily%s}%s%s%s%s Stack: %s\\%s%s%s%s" beginFrame textSize (beginCode "Python") ps endCode textSize stack heap input output endFrame
            yield slide ]
        stackTraceTables |> List.fold (+) ""
      | CSharpTypeTrace(ts,p,st) ->
        let textSize = ts.ToString()
        let stackTraces = st :: runToEnd (typeCheckCSharp p) st
        let ps = (p.AsCSharp "").TrimEnd([|'\n'|])
        let stackTraceTables = 
          [ for st in stackTraces do 
            let declarations,classes = st.AsSlideContent Dots (function Code.Hidden _ -> true | _ -> false) ConstInt (fun (c:Code) -> c.AsCSharp)
            let declarations = if declarations = "" then "" else "Declarations: " + declarations
            let classes = if classes = "" then "" else "Classes: " + classes
            let slide = sprintf @"%s\lstset{basicstyle=\ttfamily%s}%s%s%s%s %s\\%s%s" beginFrame textSize (beginCode "[Sharp]C") ps endCode textSize declarations classes endFrame
            yield slide ]
        stackTraceTables |> List.fold (+) ""
      | CSharpStateTrace(ts,p,st) ->
        let textSize = ts.ToString()
        let heapLabel,stackLabel = "Heap: ","Stack: "
        let stackTraces = st :: runToEnd (runCSharp p) st
        let ps = (p.AsCSharp "").TrimEnd([|'\n'|])
        let stackTraceTables = 
          [ for st in stackTraces do 
            let stack,heap,input,output = st.AsSlideContent Dots (function Code.Hidden _ -> true | _ -> false) (fun c -> c.AsCSharp)
            let input = if input = "" then "" else "Input: " + input + @"\\"
            let output = if output = "" then "" else "Output: " + output + @"\\"
            let heap = if heap = "" then "" else heapLabel + heap + @"\\"
            let slide = sprintf @"%s\lstset{basicstyle=\ttfamily%s}%s%s%s%s %s%s\\%s%s%s%s" beginFrame textSize (beginCode "[Sharp]C") ps endCode textSize stackLabel stack heap input output endFrame
            yield slide ]
        stackTraceTables |> List.fold (+) ""
      | LambdaStateTrace(ts,term,maxSteps,showArithmetics,showControlFlow,showLet,showPairs,showUnions) ->
        let textSize = ts.ToString()
        let states = 
          match maxSteps with
          | Some maxSteps ->
            (id,term) :: runToEnd (BetaReduction.reduce maxSteps showArithmetics showControlFlow showLet showPairs showUnions pause) (id,term)
          | _ ->
            (id,term) :: runToEnd (BetaReduction.reduce System.Int32.MaxValue showArithmetics showControlFlow showLet showPairs showUnions pause) (id,term)
        let terms = states |> List.map (fun (k,t) -> k t)
        let stackTraceTables = 
          [ for term,term' in Seq.zip terms (terms.Tail) do 
              let slide = sprintf @"%s\lstset{basicstyle=\ttfamily%s}\lstset{numbers=none}%s%s%s\pause%s%s%s%s" beginFrame textSize (beginCode "ML") (term.ToLambda) endCode (beginCode "ML") (term'.ToLambda) endCode endFrame
              yield slide ]
        let res = stackTraceTables |> List.fold (+) ""
        res
      | _ -> failwith "Unsupported"

and TypingRule =
  {
    Premises : List<string>
    Conclusion : string
  }
  with 
    override this.ToString() =
      let ps = 
        match this.Premises |> List.map ((+) "\ ") with
        | [] -> ""
        | ps -> ps |> List.reduce (fun a b -> a + "\wedge" + b)
      sprintf @"%s\frac{%s}{%s}%s" beginMath ps this.Conclusion endMath

let (!) = Text
let (!!) = InlineCode
let ItemsBlock l = l |> Items |> Block 
let TextBlock l = l |> Text |> Block 

let rec generateLatexFile author title (slides:List<SlideElement>) =
  @"\documentclass{beamer}
\usetheme[hideothersubsections]{HRTheme}
\usepackage{beamerthemeHRTheme}
\usepackage[utf8]{inputenc}
\usepackage{graphicx}
\usepackage[space]{grffile}
\usepackage{soul,xcolor}
\usepackage{listings}
\usepackage{tabularx}
\lstset{language=C,
basicstyle=\ttfamily\footnotesize,
escapeinside={(*@}{@*)},
mathescape=true,
breaklines=true}
\lstset{
  literate={ï}{{\""i}}1
           {ì}{{\`i}}1
}

\title{" + title + @"}

\author{" + author + @"}

\institute{Hogeschool Rotterdam \\ 
Rotterdam, Netherlands}

\date{}

\begin{document}
\maketitle
" + (List.map (fun x -> x.ToString()) slides |> List.fold (+) "") + @"
\begin{frame}{This is it!}
\center
\fontsize{18pt}{7.2}\selectfont
The best of luck, and thanks for the attention!
\end{frame}

\end{document}"
  