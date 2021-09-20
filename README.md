# algorithm for progressive building simplification 

For more information, refer to:Wei Z, Liu Y, Cheng L, et al. A Progressive and Combined Building Simplification Approach with Local Structure Classification and Backtracking Strategy[J]. ISPRS International Journal of Geo-Information, 2021, 10(5): 302. https://www.mdpi.com/2220-9964/10/5/302

To support the findings of our submitted paper entitled “A progressive and combined building simplification approach with local structure classification and backtracking strategy”, we implemented five building simplification algorithms (i.e., adjacent four-points algorithms (AF), recursive regression (RR), building enlargement (BE), local structure-based simplification (LS) and template matching. The other tools were implemented in C# on ArcGIS 10.2 software (ESRI, USA), each tool has a separate form to set the input, output and parameters for the algorithms.

Input: a polygon shapefile without Z value, similar as the providing data. The input is set by selecting a polygon layer in the window form.
 
Output: an output file path. The output path can be set by the button of output path setting button.
 
Parameter setting: take AF algorithm for example, minimum length of edge needs to be set (e.g. minEdge as 7.5m at scale 1: 25k). The parameter can be set by users.
 
Press “Ok” button to execute the algorithm. And a progress bar will indicate the progress. And one thing to point out: it will be better if the coordinate systems of the data applied in TS algorithm are the same.

