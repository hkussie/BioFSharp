(*** hide ***)
// This block of code is omitted in the generated HTML documentation. Use 
// it to define helpers that you do not want to show in the documentation.
#I @"../../bin/BioFSharp/net47/"
#I @"../../bin/BioFSharp.BioDB/net47/"
#I @"../../bin/BioFSharp.ImgP/net47"
#I @"../../bin/BioFSharp.IO/net47/"
#I @"../../bin/BioFSharp.Parallel/net47/"
#I @"../../bin/BioFSharp.Stats/net47/"
#I @"../../bin/BioFSharp.Vis/net47/"
#r @"../../packages/formatting/FSharp.Plotly/lib/netstandard2.0/FSharp.Plotly.dll"
#r "FSharpAux.dll"
#r "FSharpAux.IO.dll"
#r "BioFSharp.dll"
#r "BioFSharp.IO.dll"
#r "BioFSharp.Stats.dll"
open BioFSharp

open FSharpAux

(**
<table class="HeadAPI">
<td class="Head"><h1>Gene set enrichment analysis</h1></td>
<td class="API">
    <a id="APILink" href="https://csbiology.github.io/BioFSharp/reference/Ontology_enrichment.html" >&#128194;View module documentation</a>
</td>
</table>

When performing high-troughput experiments the question arises how to analyze the resulting huge data sets. 
Of course the analysis of each single item separately is possible but unefficient and cumberersome.
If you want to compare the transcript/protein composition of a system before and after a treatment it is useful
to make use of a gene set enrichment analysis (GSEA) to identify overrepresented transcript/protein groups.
The grouping can be based on MapMan- or GO-annotations or other shared properties.
For further information visit [GSEA publication](https://academic.oup.com/bioinformatics/article/23/4/401/181853).


##Method
Imagine an experiment, in which 1000 proteins were measured. Within these 1000 proteins there are 25 proteins 
of a specific functional group (heat shock response).
By treating the organism(s) 200 proteins are significantly altered. 10 of the 25 heat responsive proteins 
can be found within these 200. 

Null-Hypothesis: The number of identified altered items is due to chance.

Question: What is the probability, that these 10 or more proteins occuring in the 200 altered proteins by chance?

The hypergeomentric distribution can answer this question:

- N is the population size (1000 proteins)

- K is the number of success states in the population (25 heat responsive proteins)

- n is the number of draws (200 significantly altered proteins)

- k is the number of observed successes (10 or higher)

Result: 0.0161 = 1.6%

If the calculated probability is small, it is very unlikely, that the observed effect is due to chance. 
This implies, that under the given condition the heat response proteins are enriched.

##Usage

*)

//Usage:
//At first all items are converted into an OntologyItem type:

open BioFSharp
open BioFSharp.Stats

//Example item
//Protein:                  HSP70
//MapManAnnotation:         GMM:29.6.2.3;GMM:20.2.1
//Significantly altered?:   true

//Creates an OntologyItem with an identifier, an Ontologyterm, a group index and an arbitrary and 
//optional field. The groupIndex is based on prior analyis and defines if the items belongs to a 
//specific group. If significantly altered proteins were identified, you can assign index '1' to 
//all altered proteins and '0' to all other. The choice of the index is up to you, but you have 
//to remind it for later steps.
let item = OntologyEnrichment.createOntologyItem "Cre06.g250100" "GMM:29.6.2.3;GMM:20.2.1" 1 "HSP70"


//If more than one ontology term is possible, you can use this function to split the item, so that
//every resulting item contains just one ontology term.
let splitItems = OntologyEnrichment.splitMultipleAnnotationsBy ';' item

//create a list of all items in your experiment
let items = [splitItems(*...*)] |> Seq.concat

//When dealing with MapMan annotations all levels of the descriptions should be considered.
//GMM:26.6.2 -> [GMM:26.6.2;GMM:26.6;GMM:26] 
let expandedItems = OntologyEnrichment.expandOntologyTree items


//now the dataset is ready for performing the enrichment analysis
//deGroupIndex:         which index should be considered as success? (in this case '1')
//splitPValueThreshold: if a bin size is below this threshold, the resulting pvalue 
//                      is split up (-> increased to lower the impact of small bins) (default 5).
//minNumberInTerm:      what is the minimal number of items that should be 
//                      within a bin to consider the bin at all (default 2).
//data:                 sequence of expanded and split ontology items
let gseaResult = OntologyEnrichment.CalcOverEnrichment 1 (Some 5) (Some 2) expandedItems


let filterAndSortEnrichedModules =
    gseaResult
    |> Seq.filter (fun x -> x.PValue < 0.05)
    |> Seq.sortByDescending (fun x -> x.NumberOfDEsInBin)

//the result type contains the following fields:
//GseaResult:
//    OntologyTerm     : OntologyTerm(MapMan-Term/GO-Term/...)
//    ItemsInBin       : Sequence of single items belonging to the ontology term 
//    NumberOfDEsInBin : number of significantly altered items in 'OntologyItem' bin
//    NumberInBin      : number of items in 'OntologyItem' bin
//    TotalNumberOfDE  : number of significantly altered items
//    TotalUnivers     : number of all items (expanded)
//    PValue           : p value
