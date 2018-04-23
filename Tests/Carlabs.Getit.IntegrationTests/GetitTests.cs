﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestPlatform.ObjectModel;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Carlabs.Getit.IntegrationTests
{
    [TestClass]
    public class GetitTests
    {
        public static string RemoveWhitespace(string input)
        {
            return new string(input.ToCharArray()
                .Where(c => !Char.IsWhiteSpace(c))
                .ToArray());
        }

        [ClassInitialize()]
        public static void ClassInit(TestContext context)
        {
            // set up any needed config stuff for Getit here

            Getit.Config.SetUrl("http://192.168.1.75/clapper/web/graphql");
        }

        // Property/Method, What we are testing, Expecting

        [TestMethod]
        public void Query_SelectParams_ReturnsCorrect()
        {
            const string select = "zip";

            // Arrange
            IQuery query = Getit.Query().Select(select);

            // Assert
            Assert.AreEqual(select, query.SelectList.First());
        }

        [TestMethod]
        public void Query_Unique_ReturnsCorrect()
        {
            // Arrange
            IQuery query = Getit.Query().Select("zip");
            IQuery query1 = Getit.Query().Select("pitydodah");

            // Assert counts and not the same
            Assert.IsTrue(query.SelectList.Count == 1);
            Assert.IsTrue(query1.SelectList.Count == 1);
            Assert.AreNotEqual(query.SelectList.First(), query1.SelectList.First());
        }

        [TestMethod]
        public void Query_ToString_ReturnsCorrect()
        {
            // Arrange
            IQuery query = Getit.Query().Name("test1").Select("id");

            // Assert
            Assert.AreEqual("test1{id}", RemoveWhitespace(query.ToString()));
        }

        [TestMethod]
        public void ComplexQuery_ToString_Check()
        {
            // Arrange
            IQuery query = Getit.Query();
            IQuery subSelect = Getit.Query();

            // set up a couple of ENUMS
            EnumHelper gqlEnumEnabled = new EnumHelper().Enum("ENABLED");
            EnumHelper gqlEnumDisabled = new EnumHelper("DISABLED");

            // set up a subselection list
            List<object> subSelList = new List<object>(new object[] { "subName", "subMake", "subModel" });

            // set up a subselection parameter (where)
            // has simple string, int, and a couple of ENUMs
            Dictionary<string, object> mySubDict = new Dictionary<string, object>
            {
                {"subMake", "honda"},
                {"subState", "ca"},
                {"subLimit", 1},
                {"__debug", gqlEnumDisabled},
                {"SuperQuerySpeed", gqlEnumEnabled}
            };

            // Create a Sub Select Query
            subSelect
                .Select(subSelList)
                .Name("subDealer")
                .Where(mySubDict)
                .Comment("SubSelect Below!");

            // Add that subselect to the main select
            List<object> selList = new List<object>(new object[] { "id", subSelect, "name", "make", "model" });

            // List of ints (IDs)
            List<int> trimList = new List<int>(new[] { 43783, 43784, 43145 });

            // String List
            List<string> modelList = new List<string>(new[] { "DB7", "DB9", "Vantage" });

            // Another List but of Generic Objects that should work as strings
            List<object> recList = new List<object>(new object[] { "aa", "bb", "cc" });

            // try a dict for the typical from to with doubles
            Dictionary<string, object> recMap = new Dictionary<string, object>
            {
                {"from", 444.45},
                {"to", 555.45},
            };

            // try a dict for nested list and dict
            Dictionary<string, object> fromToPrice = new Dictionary<string, object>
            {
                {"from", 123},
                {"to", 454},
                {"recurse", recList},
                {"map", recMap}
            };

            // Even more stuff nested in the params
            Dictionary<string, object> myDict = new Dictionary<string, object>
            {
                {"make", "honda"},
                {"state", "ca"},
                {"limit", 2},
                {"trims", trimList},
                {"models", modelList},
                {"price", fromToPrice},
                {"__debug", gqlEnumEnabled},
            };

            // Generate the query with an alias and multi-line comment
            query
                .Select(selList)
                .Name("Dealer")
                .Alias("myDealerAlias")
                .Where(myDict)
                .Comment("My First F'n GQL Query with geTit\na second line of comments\nand yet another line of comments");

            // Get and pack results
            string packedResults = RemoveWhitespace(query.ToString());
            string packedCheck = RemoveWhitespace(@"
                    myDealerAlias: Dealer(make: ""honda"", state: ""ca"", limit: 2, trims:[43783, 43784, 43145], models:[""DB7"", ""DB9"", ""Vantage""],
                    price:{ from: 123, to: 454, recurse:[""aa"", ""bb"", ""cc""], map: { from: 444.45, to: 555.45} },
                    __debug: ENABLED){
                    # My First F'n GQL Query with geTit
                    # a second line of comments
                    # and yet another line of comments
                    id
                    subDealer(subMake: ""honda"", subState: ""ca"", subLimit: 1, __debug: DISABLED, SuperQuerySpeed: ENABLED){
                        # SubSelect Below!
                        subName
                        subMake
                        subModel
                    }
                    name
                    make
                    model
                }");

            // Best be the same!
            Assert.AreEqual(packedResults, packedCheck);
        }

        [TestMethod]
        public async Task Query_Get_ReturnsCorrect()
        {
            // Arrange (set for a honda endpoint or what ever vendor (makeId is used)
            // NOTE : THIS TEST WILL FAIL WITHOUT A VALID WORKING GQL ENDPOINT TO HONDA DATA
            Getit.Config.SetUrl("http://hondadevclapperng.us-east-1.elasticbeanstalk.com/graphql");

            IQuery query = Getit.Query();
            IQuery subSelect = Getit.Query();

            // Nearest Dealer has a sub-select of a dealer
            subSelect
                .Name("Dealer")
                .Select("id", "subId", "name", "make");

            // main query, with distance, and sub-select
            query
                .Name("NearestDealer")
                .Select("distance")
                .Select(subSelect)
                .Where("zip", "91302")
                .Where("makeId", 16);   // honda

            string results = await query.Get<string>();

            // Assert
            Assert.IsNotNull(results);
            Assert.IsTrue(results.IndexOf("Galpin Honda") >= 0); 
        }

        [TestMethod]
        public async Task Query_BatchGet_ReturnsCorrect()
        {
            // Arrange (set for a honda endpoint or what ever vendor (makeId is used)
            // NOTE : THIS TEST WILL FAIL WITHOUT A VALID WORKING GQL ENDPOINT TO HONDA DATA
            Getit.Config.SetUrl("http://hondadevclapperng.us-east-1.elasticbeanstalk.com/graphql");

            IQuery query = Getit.Query();
            IQuery subSelect = Getit.Query();
            IQuery batchQuery = Getit.Query();
            IQuery batchSubSelectQuery = Getit.Query();

            // Nearest Dealer has a sub-select of a dealer
            subSelect
                .Name("Dealer")
                .Select("id", "subId", "name", "make");

            // query, with distance, and sub-select
            query
                .Name("NearestDealer")
                .Alias("SecondQuery")
                .Select("distance")
                .Select(subSelect)
                .Where("zip", "91302")
                .Where("makeId", 16);   // honda makeid = 16

            // Nearest Dealer has a sub-select of a dealer
            batchSubSelectQuery
                .Name("Dealer")
                .Select("id", "subId", "name");

            // main query, with distance, and sub-select
            batchQuery
                .Name("NearestDealer")
                .Alias("BatchQuery")
                .Select("distance")
                .Select(batchSubSelectQuery)
                .Where("zip", "91302")
                .Where("makeId", 16)
                .Batch(query);

            // get the json results as a strings
            string results = await batchQuery.Get<string>();

            // Assert
            Assert.IsNotNull(results);

            // check for 2 as the query is the same, should be
            // further down the line...
            int firstIndex = results.IndexOf("Galpin Honda");
            int secondIndex = results.IndexOf("Galpin Honda", 11 + firstIndex);

            // will fail if both -1
            Assert.IsTrue(secondIndex > firstIndex);
        }
    }
}
