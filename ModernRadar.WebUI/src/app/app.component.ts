import { Component } from '@angular/core';
import { RadarMapComponent } from './components/radar-map/radar-map.component';

@Component({
    selector: 'app-root',
    standalone: true,
    imports: [RadarMapComponent],
    templateUrl: './app.component.html',
    styleUrls: ['./app.component.css']
})
export class AppComponent {
    title = 'ModernRadar.WebUI';
}
