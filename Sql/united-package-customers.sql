-- Part 3 — W3Schools Try-It editor (Northwind sample database)
-- Find all customer names and countries that used "United Package" as their shipper.

SELECT DISTINCT c.CustomerName,
                c.Country
FROM Customers AS c
JOIN Orders    AS o ON o.CustomerID = c.CustomerID
JOIN Shippers  AS s ON s.ShipperID  = o.ShipperID
WHERE s.ShipperName = 'United Package'
ORDER BY c.Country, c.CustomerName;

-- Notes on the choices:
--
-- DISTINCT      A customer may have placed several orders with the same shipper.
--               Without DISTINCT the same customer is returned once per order.
--
-- JOIN Shippers The shipper is matched on its name rather than on a hard-coded
--               ShipperID (2 in this dataset). Matching on the id would pass today
--               and silently break if the reference data changed.
--
-- ORDER BY      Not required, but a stable order makes the result readable and
--               makes it comparable between runs.
