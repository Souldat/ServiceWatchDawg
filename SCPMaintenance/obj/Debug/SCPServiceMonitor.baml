   M S B A M L   `   `   `  ����I  ESCPMaintenance, Version=1.0.0.0, Culture=neutral, PublicKeyToken=null/clr-namespace:SCPMaintenanceSCPMaintenance  5   	   &     SCPMaintenance.SCPServiceMonitor   5   	   \ XPresentationFramework, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35R NWindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35W SPresentationCore, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35D 9http://schemas.microsoft.com/winfx/2006/xaml/presentation    5   	   R NSystem.Xaml, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089:x,http://schemas.microsoft.com/winfx/2006/xaml     5   	   @d2http://schemas.microsoft.com/expression/blend/2008     5   	   Jmc;http://schemas.openxmlformats.org/markup-compatibility/2006     5   	   .localclr-namespace:SCPMaintenance     5   	     � Title$  SCP Service Monitor��5   	   $��432.557��6%   $��624.356��66   .��5	      � $��0,0,2,0q�6    � ColumnDefinitions 5
   
   �� 5      �� 5      $��0* �6   .+�5   
   �� -   5         ��Name$ listBoxServices��5      $
��Left=�6+   $	��350��6F   $��	10,36,0,0q�6S   $	��Top=�6f   $	��241��6~    �� SelectionMode$ Multiple=�6�   �� 5   
   $��Services��6   $
��Left=�6#   $��	10,10,0,0q�6>   $	��Top=�6Q    J� RenderTransformOrigin$ 0.47,-1.661&�6i   $��26��6�   $��52��6�   �� 5   
   -   5         ��Name$ btnStartMonitor��5      $��Start Monitor��6*   $
��Left=�6B   $��
504,41,0,0q�6]   $	��Top=�6q   $��97��6�   $��25��6�   �� 5   
   -   5        $ listBoxFiles��5      $
��Left=�6(   $	��101��6C   $��
271,36,0,0q�6P   $	��Top=�6d   $	��199��6|   �� 5   
   $��Files to Monitor��6   $
��Left=�6+   $��
271,10,0,0q�6F   $	��Top=�6Z   $��26��6r   $��93��6~   �� 5   
   -   5        $ btnFileSelect��5      $��Select Files��6(   $
��Left=�6?   $��271,143,0,0q�6Z   $	��Top=�6o   $��64��6�   $��23��6�   �� 5   
   -   5         ��Name$ txtBoxSearchText��5      $
��Left=�6,   $	��101��6G   $��271,192,0,0q�6T    �� TextWrapping$
 Wrap=�6i   $	��Top=�6}   $	��193��6�   �� 5   
   $��Text To Search For��6   $
��Left=�6-   $��271,166,0,0q�6H   $	��Top=�6]   $��26��6u   $	��108��6�   �� 5   
   -   5        $ txtBoxMonitor��5      $
��Left=�6)   $��62��6D   $��271,324,0,0q�6P   $
 Wrap=�6e   $	��Top=�6y   $	��193��6�    �� 
IsReadOnly . 6�   �� 5   
   -   5        $ btnStopMonitor��5      $��Stop Monitor��6)   $
��Left=�6@   $��
504,81,0,0q�6[   $	��Top=�6o   $��97��6�   $��25��6�   6�   �� 5   
   -   5        	 ��Name$	 
lblPending��5      $�� ��6$   $��Center=�6/   $
��Left=�6S   $��509,123,0,0q�6n   $	��Top=�6�   $��97��6�   $��26��6�   �� 5   
   $��Status��6   $
��Left=�6!   $��271,298,0,0q�6<   $	��Top=�6Q   $��26��6i   $��42��6u   �� 5   
   -	   5        $ btnClearFiles��5      $��Clear��6(   $
��Left=�68   $��353,143,0,0q�6S   $	��Top=�6h   $��64��6�   $��23��6�   6�   5      5      