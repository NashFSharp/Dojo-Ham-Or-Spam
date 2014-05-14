﻿(* **********************************************
0. PREFACE: GETTING DATA

First let's grab some data!
The following code is boring, but will spare you some time
loading up a training and validation set.
The dataset is a collection of real SMS messages, 
marked as "Spam" or "Ham".
The original dataset has been taken from 
the UC Irvine Machine Learning Repository:
http://archive.ics.uci.edu/ml/datasets/SMS+Spam+Collection
*)



// TODO: SIT BACK, RELAX, RUN THE CODE BELOW :) 

let source = __SOURCE_DIRECTORY__

#load "NaiveBayes.fs"
open MachineLearning.NaiveBayes

open System
open System.IO
open System.Text
open System.Text.RegularExpressions

let trainingPath = source + "\SpamTraining"
let validationPath = source + "\SpamValidation"

// we define 2 classes, Ham or Spam
type Class = Spam | Ham

let spamOrHam (line: string) =
    if line.StartsWith("ham") then (Ham, line.Remove(0,4))
    elif line.StartsWith("spam") then (Spam, line.Remove(0,5))
    else failwith "What is this?"

let read path = 
    File.ReadAllLines(path)
    |> Array.map spamOrHam

let trainingSample = read trainingPath
let validationSample = read validationPath



(* **********************************************
CHAPTER 1: GET TO KNOW YOUR DATA

It's always a good idea to spend some time
to know your data, and become "intimate" with it.
The more you understand it, the better you can
help your machine get smart!
For instance, can you spot some patterns
in the Spam SMS, and get a sense for why
they are considered Spam?
*)


// let's look at the 20 first "ham" items
let ham_20 = 
    trainingSample
    |> Array.filter (fun (cl, txt) -> cl = Ham)
    |> Seq.take 20
    |> Seq.iter (fun (cl, txt) -> printfn "%s" txt)


// TODO: DISPLAY 20 FIRST SPAM SMS
let spam_20 = 
    trainingSample
    |> Array.filter (fun (cl, txt) -> cl = Spam)
    |> Seq.take 20
    |> Seq.iter (fun (cl, txt) -> printfn "%s" txt)


(* **********************************************
CHAPTER 2: ESTABLISH A BASELINE

It is crucial to establish a baseline for what
"a good/bad prediction" is. What we have to beat
here is a "naive" prediction, the most likely class.

What is the probability that a SMS message 
from the training set is spam or ham?
How about the validation sample?
Proba(SMS is Spam) = count(Spam SMS) / count(SMS)
*)


// let's split the sample into ham vs. spam:
let ham, spam = trainingSample |> Array.partition (fun (cl,txt) -> cl = Ham)


// TODO: COMPUTE PROBABILITY OF HAM, SPAM
let ``P(ham)``, ``P(spam)`` =     
    float(ham.Length)/float(trainingSample.Length),
    float(spam.Length)/float(trainingSample.Length)

(* **********************************************
CHAPTER 3: CLASSIFY A MESSAGE BASED ON SINGLE WORD / TOKEN

What is the probability that a spam SMS message
contains the word "ringtone"? "mom"? "800"? 
Quick math recap, just in case: 
*)
// Proba(Spam SMS contains "ringtone") = 
//    count(Spam SMS containing "ringtone") / count(Spam SMS)
// Proba(SMS is Spam if contains "ringtone") =
//    Proba(SMS contains "ringtone" if it is Spam) * 
//    Proba(SMS is Spam) / Proba(SMS contains "ringtone")

(*
This is a direct application of Bayes' Theorem:
(See Chapter 3)
Note that if we just want to decide whether
a message is ham or spam, we can ignore the 
Proba(SMS contains "chat") part.
*)


let containsToken (token:string) (txt:string) =
    txt.Contains(token)


// THIS IS WHERE YOU WORK YOUR MAGIC...
let hamOrSpamIfContains (sample:(Class*string)[]) (token:string) =
    // split the sample in ham vs. spam
    let ham,spam = 
        sample 
        |> Array.partition (fun (cl,txt) -> cl = Ham)
    
    // Compute probability of SMS containing token
    let pToken = 
        let withToken = 
            sample 
            |> Array.filter (fun (cl,txt) -> containsToken token txt)
        float (Array.length withToken) / float (Array.length sample)

    // TODO FIX THIS SECTION!

    let pHam = float(ham.Length)/float(sample.Length) 
    let pHamContainsToken = 
        (ham |> Array.filter (fun (_,s) -> containsToken token s) |> Seq.length |> float)/
        (ham |> Seq.length |> float)

    let pSpam = 1. - pHam 
    let pSpamContainsToken = 
        (spam |> Array.filter (fun (_,s) -> containsToken token s) |> Seq.length |> float)/
        (spam |> Seq.length |> float)

    // ... and enjoy the results of your hard labor:
    // this is the application of Bayes' Theorem
    let pHamIfContainsToken = pHamContainsToken * pHam / pToken
    let pSpamIfContainsToken = pSpamContainsToken * pSpam / pToken
    (pHamIfContainsToken, pSpamIfContainsToken)



// TODO: PROBA THAT MESSAGE IS HAM OR SPAM
// IF CONTAINS "ring", "800", "chat", "text" ...
let ``P(ham|ring)``, ``P(spam|ring)`` = "ring" |> hamOrSpamIfContains trainingSample
let ``P(ham|800)``,``P(spam|800)`` = "800" |> hamOrSpamIfContains trainingSample
let ``P(ham|chat)``,``P(spam|chat)`` = "chat" |> hamOrSpamIfContains trainingSample
let ``P(ham|text)``,``P(spam|text)`` = "text" |> hamOrSpamIfContains trainingSample


(* **********************************************
CHAPTER 4: NAIVE BAYES CLASSIFIER DEMO

The Naive Bayes classifier uses the same idea,
but instead of using one token, it will combine
the probabilities of each token into one aggregate
probability.
Instead of coding it from scratch, we'll use then
basic implementation from the file NaiveBayes.fs
Below is an illustration on how to train a classifier,
using an arbitrary list of tokens, and some of the 
built-in functions in NaiveBayes.fs.
*)



// TODO: SIT BACK, RELAX, RUN THE CODE BELOW :) 



// select what tokens to use:
// a large part of how good the classifier is,
// depends on chosing good tokens.
let demoTokens = Set.ofList [ "chat"; "800"; "mom"; "ringtone"; "prize"; "you"]
// train a classifier using a sample and tokens
let demoClassifier = classifier bagOfWords trainingSample demoTokens

// look at what the classifier is doing :)
validationSample.[0..19]
|> Array.iter (fun (cl, text) -> printfn "%A -> %A / %s" cl (demoClassifier text) text)

// Let's compute the % correctly classified;
// not too shabby for a model using only 6 semi-random words!
validationSample
|> Seq.averageBy (fun (cl,txt) -> if cl = demoClassifier txt then 1. else 0.)
|> printfn "Correct: %.4f"


(* **********************************************
CHAPTER 6: SENTIMENT ANALYSIS

Looking at what words are frequently used
in different groups can give insight into
what "defines" these groups. This is often
referred to a "sentiment" analysis.
*)

// Extract tokens from training sample
let tokens = extractWords trainingSample
// Compute count of token in sample
let frequency = bagOfWords (prepare trainingSample) tokens



// TODO: 10 MOST FREQUENT TOKENS IN HAM? IN SPAM?
// Hint: Map.toSeq will convert a Map<a,b> 
// into a sequence of tuples (a,b), which can
// then be sorted using Seq.sortBy
let mostFrequent =
    let topHam = 
        bagOfWords (prepare ham) tokens
        |> Map.toSeq
        |> Seq.sortBy (fun (_,i) -> -1*i)
        |> Seq.take 10
        |> Seq.map (fst)
        |> Array.ofSeq
    printfn "Most frequent in Ham"
    topHam |> Array.iter (printfn "%s")
    printfn ""

    let topSpam = 
        bagOfWords (prepare spam) tokens
            |> Map.toSeq
            |> Seq.sortBy (fun (_,i) -> -1*i)
            |> Seq.take 10
            |> Seq.map (fst)
            |> Array.ofSeq
    printfn "Most frequent in Spam"
    topSpam |> Array.iter (printfn "%s")
    printfn ""



(* **********************************************
CHAPTER 6: STOP WORDS!

Did you note that some of the top words in
both Ham and Spam are just very common English
words, like "i", "you", "to"... ?
These are probably not very informative, and 
often called "stop words".
Let's create a "clean" list of tokens by removing
the stop words, and check our top tokens again.
*)

// http://www.textfixer.com/resources/common-english-words.txt
let stopWords = 
    let asString = "a,able,about,across,after,all,almost,also,am,among,an,and,any,are,as,at,be,because,been,but,by,can,cannot,could,dear,did,do,does,either,else,ever,every,for,from,get,got,had,has,have,he,her,hers,him,his,how,however,i,if,in,into,is,it,its,just,least,let,like,likely,may,me,might,most,must,my,neither,no,nor,not,of,off,often,on,only,or,other,our,own,rather,said,say,says,she,should,since,so,some,than,that,the,their,them,then,there,these,they,this,tis,to,too,twas,us,wants,was,we,were,what,when,where,which,while,who,whom,why,will,with,would,yet,you,your"
    asString.Split(',') |> Set.ofArray



// TODO: CLEAN TOKENS = TOKENS - STOP WORDS
let cleanTokens = (tokens, stopWords) ||> Set.difference

// TODO, AGAIN: MOST FREQUENT TOKENS IN HAM, SPAM?

let frequentHamTokens = 
    bagOfWords (prepare ham) cleanTokens
        |> Map.toSeq
        |> Seq.sortBy (fun (_,i) -> -i)        
        |> Seq.map (fst)        

let frequentSpamTokens =  
    bagOfWords (prepare spam) cleanTokens
        |> Map.toSeq
        |> Seq.sortBy (fun (_,i) -> -i)        
        |> Seq.map (fst)


(* **********************************************
CHAPTER 7: OUR FIRST CLASSIFIER

Now that we have a decent list of tokens 
to start with, let's train a classifier.
*)



// TODO: PICK TOP 10 SPAM + TOP 10 HAM "CLEAN" TOKENS,
// AND TRAIN CLASSIFIER WITH THESE TOKENS
let take n seq = seq |> Seq.take n |> Set.ofSeq

let betterTokens = (frequentHamTokens |> take 10, frequentSpamTokens |> take 10) ||> Set.union
// train a classifier using a sample and tokens
let betterClassifier = classifier bagOfWords trainingSample betterTokens


validationSample.[0..19]
|> Array.iter (fun (cl, text) -> printfn "%A -> %A / %s" cl (betterClassifier text) text)

validationSample
|> Seq.averageBy (fun (cl,txt) -> if cl = betterClassifier txt then 1. else 0.)
|> printfn "Correct: %.4f"



(* **********************************************
CHAPTER 8: BUT HERE'S MY NUMBER, SO CALL ME MAYBE

Remember in Chapter 3, when we checked for messages
containing "800"? Did you notice how many Spam SMSs
contain numbers (phone or text)?  
Can we make them into a feature / token?
*)

let numbersRegex = Regex(@"\d{3,}")
let replaceNumbers (text: string) = numbersRegex.Replace(text, "__number__")
let exampleReplacement = "Call 1800123456 for your free spam" |> replaceNumbers



// TODO: PRE-PROCESS TRAINING SET AND VALIDATION SET
// TO DEAL WITH "NUMBERS": REPLACE NUMBERS WITH __number__
spam |> Array.filter (fun (_,s) -> numbersRegex.IsMatch(s)) |> Seq.length // 469
ham |> Array.filter (fun (_,s) -> numbersRegex.IsMatch(s)) |> Seq.length // 28


let training = trainingSample |> Array.map (fun (c,s) -> c, replaceNumbers(s)) 
let validation = validationSample |> Array.map (fun (c,s) -> c, replaceNumbers(s))



// TODO: TRAIN A CLASSIFIER ON PRE-PROCESSED
// TRAINING SET, AND EVALUATE QUALITY BY
// COMPUTING % CORRECTLY CLASSIFIED ON VALIDATION SET
let betterClassifier' = classifier bagOfWords training (betterTokens.Add("__number__"))
validation.[20..29]
|> Array.iter (fun (cl, text) -> printfn "%A -> %A / %s" cl (betterClassifier' text) text)

validation
|> Seq.averageBy (fun (cl,txt) -> if cl = betterClassifier' txt then 1. else 0.)
|> printfn "Correct: %.4f"

validation
|> Seq.filter (fun (c,s) -> c <> (betterClassifier' s)) 
|> Seq.sortBy (fst)
|> Seq.iter (fun (c,s) -> printfn "%A -> %A / %s" c (betterClassifier' s) s)

let missSpam =
    validation
    |> Array.filter (fun (c,s) -> c <> (betterClassifier' s) && c = Spam)

missSpam |> Seq.iter (fun (c,s) -> printfn "%A -> %A / %s" c (betterClassifier' s) s)

(* **********************************************
EPILOGUE...
- ANY THOUGHTS ON HOW YOU COULD IMPROVE THAT MODEL FURTHER?
- HOW MUCH HAM, SPAM IS MIS-CLASSIFIED? WHAT'S WORSE?
*)

// The more number of tokens, the better the classifier until..
[10;20;100;500;2500;5000]
|> Seq.map (fun n -> n,(frequentHamTokens |> take n, frequentSpamTokens |> take n) ||> Set.union)
|> Seq.map (fun (n,tokens) -> n, classifier bagOfWords trainingSample tokens)
|> Seq.iter (fun (n,c) -> 
        validationSample
        |> Seq.averageBy (fun (cl,txt) -> if cl = c txt then 1. else 0.)
        |> printfn "Tokens: %d Correct: %.4f" n
    )

[10;20;100;500;2500;5000]
|> Seq.map (fun n -> n,(frequentHamTokens |> take n, frequentSpamTokens |> take n) ||> Set.union)
|> Seq.map (fun (n,tokens) -> n, classifier bagOfWords training (tokens.Add("__number__")))
|> Seq.iter (fun (n,c) -> 
        validation
        |> Seq.averageBy (fun (cl,txt) -> if cl = c txt then 1. else 0.)
        |> printfn "Tokens: %d Correct: %.4f" n
    )


