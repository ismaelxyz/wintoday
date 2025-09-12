import './assets/main.css'
import { createApp } from 'vue'
import App from './App.vue'
import { Roulette } from 'vue3-roulette'

createApp(App)
    .component('Roulette', Roulette)
    .mount('#app')
