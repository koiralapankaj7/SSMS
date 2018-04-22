﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Services;
using System.Web.UI;
using System.Web.UI.WebControls;

namespace SSMS {
    public partial class Stock : System.Web.UI.Page {

        protected static string connectingStringSSMS = "Data Source=DESKTOP-1NMRQA9\\SQLEXPRESS; Initial Catalog=SSMS; Integrated Security=True; MultipleActiveResultSets=true";

        protected void Page_Load(object sender, EventArgs e) {

            try {

                SqlConnection connection = new SqlConnection(connectingStringSSMS);
                connection.Open();
                Response.Write("Connected");

                SqlCommand cmdSelectStockDetails = new SqlCommand {
                    Connection = connection,
                    CommandText = "SELECT stock_id, arrived_quantity, stock_quantity, arrived_date, product_id " +
                                    "FROM stock",
                    CommandType = CommandType.Text
                };

                // Items table start
                SqlDataReader stockDataReader = cmdSelectStockDetails.ExecuteReader();
                StringBuilder stockTable = new StringBuilder();
                while (stockDataReader.Read()) {

                    stockTable.Append("<tr>");
                    stockTable.Append("<td class='color-blue-grey-lighter'>" + stockDataReader.GetValue(0) + "</td>");
                    stockTable.Append("<td>" + GetProductName(connection, stockDataReader.GetValue(4)) + "</td>");
                    stockTable.Append("<td>" + stockDataReader.GetValue(1) + "</td>");
                    stockTable.Append("<td>" + stockDataReader.GetValue(2) + "</td>");
                    stockTable.Append("<td>" + stockDataReader.GetValue(3) + "</td>");
                    stockTable.Append("</tr>");

                }
                stockDataReader.Close();
                StockTablePlaceHolder.Controls.Add(new Literal { Text = stockTable.ToString() }); ;
                // Items table end


                connection.Close();
                connection.Dispose();

            } catch (Exception exception) {
                Response.Write("<script>alert(' " + exception.Message + "')</script>");
            }

        }

        // Insert items
        [WebMethod]
        public static string InsertIntoStock(string pId, string quantity, string arrivedDate, string uId) {
        
            try {

                SqlConnection connection = new SqlConnection(connectingStringSSMS);
                connection.Open();

                int stockQuantity = Convert.ToInt32(quantity) + GetStockQuantity(pId, connection);

                // Change date format form ss/mm/yy to yy/mm/dd
                // Microsoft SQL support yy/mm//dd format
                string[] date = arrivedDate.Split('/');
                string arrDate = date[2] + "/" + date[1] + "/" + date[0];

                // For the purpose of stock primary key auto increment 
                int stockId = 0;
                SqlCommand cmdStockId = new SqlCommand {
                    Connection = connection,
                    CommandText = "SELECT MAX(stock_id) FROM stock",
                    CommandType = CommandType.Text
                };

                SqlDataReader stockIdReader = cmdStockId.ExecuteReader();
                while (stockIdReader.Read()) {

                    if (stockIdReader.GetValue(0) == DBNull.Value) {
                        stockId = 1;
                    } else {
                        stockId = Convert.ToInt32(stockIdReader.GetValue(0)) + 1;
                    }

                }
                // Remember to close reader as soon as it completes its task. This is important. Otherwise it will throw 
                // "there is already an open datareader associated with this command which must be closed first." error at run time.
                stockIdReader.Close();

                SqlCommand cmdInsertIntoStock = new SqlCommand {
                    Connection = connection,
                    CommandText = "INSERT INTO stock (stock_id, arrived_quantity, stock_quantity, arrived_date, product_id) " +
                    "VALUES (" + stockId + ", " + Convert.ToInt32(quantity) + ", " + stockQuantity + ", '" + arrDate + "', " + Convert.ToInt32(pId) + ")",
                    CommandType = CommandType.Text
                };

                SqlCommand cmdInsertIntoStockManager = new SqlCommand {
                    Connection = connection,
                    CommandText = "INSERT INTO stock_manager (user_id, stock_id) " +
                    "VALUES (" + Convert.ToInt32(uId) + ", " + stockId + ")",
                    CommandType = CommandType.Text
                };

                int stockCount = cmdInsertIntoStock.ExecuteNonQuery();
                int stockManagerCount = cmdInsertIntoStockManager.ExecuteNonQuery();


                connection.Close();
                connection.Dispose();

                if (stockCount == 1 && stockManagerCount == 1) {
                    return ("1");
                } else {
                    return ("0");
                }

            } catch (Exception e) {
                return e.Message;
            }

        }

        // Insert items
        [WebMethod]
        public static string GetProductAndUser() {

            try {

                SqlConnection connection = new SqlConnection(connectingStringSSMS);
                connection.Open();

                // Get product
                SqlCommand cmdGetCategoryName = new SqlCommand {
                    Connection = connection,
                    CommandText = "SELECT product_id, item_name FROM product_details",
                    CommandType = CommandType.Text
                };

                // Get users
                SqlCommand cmdGetUsersName = new SqlCommand {
                    Connection = connection,
                    CommandText = "SELECT user_id, full_name FROM users",
                    CommandType = CommandType.Text
                };

                // Options for category
                SqlDataReader productDataReader = cmdGetCategoryName.ExecuteReader();
                StringBuilder productOption = new StringBuilder();
                while (productDataReader.Read()) {
                    productOption.Append("<option value='" + productDataReader.GetValue(0) + "'>" + productDataReader.GetValue(1) + "</option>");
                }
                productDataReader.Close();

                // Options for users
                SqlDataReader usersDataReader = cmdGetUsersName.ExecuteReader();
                StringBuilder usersOption = new StringBuilder();
                while (usersDataReader.Read()) {
                    usersOption.Append("<option value='" + usersDataReader.GetValue(0) + "'>" + usersDataReader.GetValue(1) + "</option>");
                }
                usersDataReader.Close();

                connection.Close();
                connection.Dispose();

                string optionCollection = productOption.ToString()+ "##" + usersOption.ToString();

                return optionCollection;

            } catch (Exception e) {
                return e.Message;
            }

        }

        private static int GetStockQuantity(Object id, SqlConnection connection) {

            int productId = Convert.ToInt32(id);
            int quantity = 0;

            SqlCommand cmdGetProductQuantity = new SqlCommand {
                Connection = connection,
                CommandText = "SELECT stock_quantity FROM stock WHERE product_id = " + productId + "",
                CommandType = CommandType.Text
            };

            SqlDataReader quantityReader = cmdGetProductQuantity.ExecuteReader();
            while (quantityReader.Read()) {

                if(!(quantityReader.GetValue(0) is DBNull)) {
                    quantity = Convert.ToInt32(quantityReader.GetValue(0));
                }
                
            }
            quantityReader.Close();

            return quantity;

        }

        // Get product name by its id
        private static string GetProductName(SqlConnection connection, Object objId) {

            if (!(objId is DBNull)) {

                int id = Convert.ToInt32(objId);
                string productName = null;

                SqlCommand cmdGetcategoryName = new SqlCommand {
                    Connection = connection,
                    CommandText = "SELECT item_name FROM product_details WHERE product_id = " + id + "",
                    CommandType = CommandType.Text
                };

                SqlDataReader categoryDataReader = cmdGetcategoryName.ExecuteReader();
                while (categoryDataReader.Read()) {
                    productName = categoryDataReader.GetValue(0).ToString();
                }
                categoryDataReader.Close();

                return productName;

            } else {
                return "N.A";
            }

        }

    }

}