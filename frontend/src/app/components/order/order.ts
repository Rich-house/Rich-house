import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterModule } from '@angular/router';
import { ProductsService } from '../../Services/product';
import { ChangeDetectorRef } from '@angular/core';

@Component({
  selector: 'app-order',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './order.html',
  styleUrls: ['./order.css']
})
export class orderComponent implements OnInit {
  orderInfo = { shippingAddress: '', city: '', phone: '' };
  cartData: any = null;
  isSubmitting = false;
  showSuccess = false;

  constructor(private productService: ProductsService, private cdr: ChangeDetectorRef) {}

  ngOnInit(): void {
    this.loadOrderSummary();
  }

  loadOrderSummary() {
    this.productService.getCart().subscribe({
      next: (res) => {
        this.cartData = res;
        this.cdr.detectChanges(); 
      },
      error: (err) => {
        console.error(err);
      }
    });
  }

  confirmOrder() {
    if (!this.orderInfo.shippingAddress || !this.orderInfo.city || !this.orderInfo.phone) return;
    
    this.isSubmitting = true;
    
    this.productService.submitOrder(this.orderInfo).subscribe({
      next: (res: any) => { 
        this.isSubmitting = false;
        
        if (res && res.url) {
          window.location.href = res.url;
        } else {
          this.showSuccess = true;
          this.cdr.detectChanges(); 
        }
      },
      error: (err) => {
        this.isSubmitting = false;
        console.error('Error submitting order:', err);
        this.cdr.detectChanges();
      }
    });
  }
}