USE [OdataToEntity]
GO
/****** Object:  Table [dbo].[Categories]    Script Date: 17.03.2017 17:33:17 ******/
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
SET ANSI_PADDING ON
GO

DROP PROCEDURE IF EXISTS dbo.GetOrders;
DROP PROCEDURE IF EXISTS dbo.ResetDb;

DROP TABLE IF EXISTS dbo.Categories;
DROP TABLE IF EXISTS dbo.OrderItems;
DROP TABLE IF EXISTS dbo.Orders;
DROP TABLE IF EXISTS dbo.Customers;
GO

CREATE TABLE [dbo].[Categories](
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](128) NOT NULL,
	[ParentId] [int] NULL,
 CONSTRAINT [PK_Categories] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

/****** Object:  Table [dbo].[Customers]    Script Date: 17.03.2017 17:33:17 ******/
CREATE TABLE [dbo].[Customers](
	[Address] [varchar](256) NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](128) NOT NULL,
	[Sex] [int] NULL,
 CONSTRAINT [PK_Customer] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[OrderItems](
	[Count] [int] NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[OrderId] [int] NOT NULL,
	[Price] [decimal](18, 2) NULL,
	[Product] [varchar](256) NOT NULL,
 CONSTRAINT [PK_OrderItem] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

CREATE TABLE [dbo].[Orders](
	[AltCustomerId] [int] NULL,
	[CustomerId] [int] NOT NULL,
	[Date] [datetimeoffset](7) NULL,
	[Id] [int] IDENTITY(1,1) NOT NULL,
	[Name] [varchar](256) NOT NULL,
	[Status] [int] NOT NULL,
 CONSTRAINT [PK_Orders] PRIMARY KEY CLUSTERED 
(
	[Id] ASC
)WITH (PAD_INDEX = OFF, STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF, ALLOW_ROW_LOCKS = ON, ALLOW_PAGE_LOCKS = ON) ON [PRIMARY]
) ON [PRIMARY]
GO

ALTER TABLE [dbo].[Categories]  WITH CHECK ADD  CONSTRAINT [FK_Categories_Categories] FOREIGN KEY([ParentId])
REFERENCES [dbo].[Categories] ([Id])
GO
ALTER TABLE [dbo].[Categories] CHECK CONSTRAINT [FK_Categories_Categories]
GO
ALTER TABLE [dbo].[OrderItems]  WITH CHECK ADD  CONSTRAINT [FK_OrderItem_Order] FOREIGN KEY([OrderId])
REFERENCES [dbo].[Orders] ([Id])
GO
ALTER TABLE [dbo].[OrderItems] CHECK CONSTRAINT [FK_OrderItem_Order]
GO
ALTER TABLE [dbo].[Orders]  WITH CHECK ADD  CONSTRAINT [FK_Orders_AltCustomers] FOREIGN KEY([AltCustomerId])
REFERENCES [dbo].[Customers] ([Id])
GO
ALTER TABLE [dbo].[Orders] CHECK CONSTRAINT [FK_Orders_AltCustomers]
GO
ALTER TABLE [dbo].[Orders]  WITH CHECK ADD  CONSTRAINT [FK_Orders_Customers] FOREIGN KEY([CustomerId])
REFERENCES [dbo].[Customers] ([Id])
GO
ALTER TABLE [dbo].[Orders] CHECK CONSTRAINT [FK_Orders_Customers]
GO

/****** Object:  StoredProcedure [dbo].[GetOrders]    Script Date: 17.03.2017 17:33:17 ******/
SET QUOTED_IDENTIFIER ON
GO

CREATE procedure [dbo].[GetOrders]
  @id int,
  @name varchar(256),
  @status int
as
begin
	set nocount on;

	if @id is null and @name is null and @status is null
	begin
	  select * from dbo.Orders;
	  return;
	end;

	if not @id is null
	begin
	  select * from dbo.Orders where Id = @id;
	  return;
	end;

	if not @name is null
	begin
	  select * from dbo.Orders where Name = @name;
	  return;
	end;

	if not @status is null
	begin
	  select * from dbo.Orders where Status = @status;
	  return;
	end;
end
GO

/****** Object:  StoredProcedure [dbo].[ResetDb]    Script Date: 17.03.2017 17:33:17 ******/
CREATE procedure [dbo].[ResetDb]
as
begin
	set nocount on;

	delete from dbo.OrderItems;
	delete from dbo.Orders;
	delete from dbo.Customers;
	delete from dbo.Categories;

	dbcc checkident('dbo.OrderItems', reseed, 0);
	dbcc checkident('dbo.Orders', reseed, 0);
	dbcc checkident('dbo.Customers', reseed, 0);
	dbcc checkident('dbo.Categories', reseed, 0);
end
GO
