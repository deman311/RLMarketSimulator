﻿using System;
using UnityEngine;

public class Product
{
	public string name;
	private float price;
	public float Price
	{
		get { return price; }
		set
		{
			float pPrice = GameObject.Find("SimulationController").GetComponent<StockManager>().GetProductionPrice(name);
			if (value < pPrice)
				value = pPrice;
			price = value;
		}
	}
	public int amount;
	public int invest_tend { get; set; }

	public Product(string name, int amount = 0, float price = 0, int invest_tend = 0)
    {
		this.name = name;
		this.Price = price;
		this.amount = amount;
		this.invest_tend = invest_tend;
    }

    public override bool Equals(object obj)
    {
		if (((Product)obj).name.Equals(name))
			return true;
		return false;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string ToString()
    {
		return name + ": " + price;
    }

	public Product SetInvestmentTendency(int it)
    {
		invest_tend = it;
		return this;
    }
}